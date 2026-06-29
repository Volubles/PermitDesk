using Microsoft.Playwright;
using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Lantmateriet;

public class PlaywrightLantmaterietSubmitter(
    ILogger<PlaywrightLantmaterietSubmitter> logger) : ILantmaterietSubmitter
{
    private const string FormUrl = "https://www.lantmateriet.se/sv/spridningstillstand/ansokan/";

    // Episerver field names extracted from the form source. Stable as long as
    // Lantmäteriet doesn't rebuild the form in the CMS. Each fill is wrapped
    // in FillIfPresent so a missing/renamed field logs a warning instead of
    // crashing the whole submission.
    private static class Fields
    {
        // Step 1 — Sökande
        public const string ApplicantType = "__field_47689";        // radio: företag / privatperson
        public const string AddressLocale = "__field_47714";        // radio: Svensk / Utland
        public const string CompanyName = "__field_47671";
        public const string Department = "__field_47715";
        public const string Attention = "__field_47685";
        public const string AddressLine = "__field_47672";
        public const string AddressLine2 = "__field_47675";
        public const string PostalCodeSe = "__field_47673";
        public const string CitySe = "__field_47717";

        // Step 1 — Kontaktperson
        public const string FirstName = "__field_47669";
        public const string LastName = "__field_47686";
        public const string Email = "__field_47666";
        public const string EmailConfirm = "__field_55393";
        public const string Phone = "__field_47667";

        // Step 3 — Map
        public const string MapTextarea = "__field_47718";

        // Generic
        public const string MapButton = "mapsupport";
    }

    public async Task RunAsync(SubmissionSession session)
    {
        var ct = session.CancellationToken;
        IPlaywright? playwright = null;
        IBrowser? browser = null;

        try
        {
            session.SetStep(SubmissionStep.LaunchingBrowser, "Startar webbläsare...");

            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new()
            {
                Headless = false,
                Args = ["--start-maximized"],
            });

            var context = await browser.NewContextAsync(new() { ViewportSize = ViewportSize.NoViewport });
            var page = await context.NewPageAsync();

            await page.GotoAsync(FormUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });

            // Handle cookie consent if it appears (Lantmäteriet uses a banner).
            await TryDismissCookieBannerAsync(page);

            session.SetStep(SubmissionStep.FillingForm, "Fyller i formuläret...");

            await FillStep1Async(page, session.Context, ct);
            await ClickNextAsync(page, ct);

            await FillStep2Async(page, session.Context, ct);
            await ClickNextAsync(page, ct);

            session.SetStep(SubmissionStep.AwaitingMap,
                "Klicka 'Markera på karta', rita området och stäng popupen.");

            await page.Locator($"input[name='{Fields.MapButton}']").ClickAsync(new() { Force = true });

            // Wait for the broker to draw the polygon. The map popup writes coords
            // back into the parent page's hidden textarea via postMessage, and the
            // js-mapsupport-mapok div switches from class="hidden" to visible.
            await page.Locator("#js-mapsupport-mapok").WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 30 * 60 * 1000, // 30 minutes for the map step
            });

            await ClickNextAsync(page, ct);
            await FillStep4Async(page, session.Context, ct);

            session.SetStep(SubmissionStep.AwaitingConfirmation,
                "Verifiera värdena i webbläsaren. Klicka 'Skicka in nu' när du är redo.");

            var confirmed = await session.WaitForConfirmationAsync();
            if (!confirmed)
            {
                logger.LogInformation("Submission for case {CaseId} cancelled before final submit", session.CaseId);
                return;
            }

            session.SetStep(SubmissionStep.Submitting, "Skickar in ansökan...");

            await page.Locator("button[data-f-type='submitbutton']").ClickAsync();

            // Wait for the form's success state. Lantmäteriet's CMS swaps the form
            // for a confirmation block — we screenshot whatever the page is showing
            // after submit.
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 60_000 });

            var screenshot = await page.ScreenshotAsync(new() { FullPage = true, Type = ScreenshotType.Png });
            session.SetScreenshot(screenshot);

            session.SetStep(SubmissionStep.Completed, "Ansökan inskickad. Bekräftelse sparad som bild.");
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Submission for case {CaseId} cancelled", session.CaseId);
            session.SetStep(SubmissionStep.Cancelled, "Avbruten.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Submission for case {CaseId} failed", session.CaseId);
            session.Fail(ex.Message);
        }
        finally
        {
            try { if (browser is not null) await browser.CloseAsync(); } catch { }
            playwright?.Dispose();
        }
    }

    private async Task FillStep1Async(IPage page, SubmissionContext ctx, CancellationToken ct)
    {
        // Sökande — företag, svensk adress
        await SelectRadioIfPresentAsync(page, Fields.ApplicantType, "företag");
        await SelectRadioIfPresentAsync(page, Fields.AddressLocale, "Svensk");

        await FillIfPresentAsync(page, Fields.CompanyName, ctx.Applicant.CompanyName);
        await FillIfPresentAsync(page, Fields.Department, ctx.Applicant.Department);
        await FillIfPresentAsync(page, Fields.AddressLine, ctx.Applicant.Address);
        await FillIfPresentAsync(page, Fields.PostalCodeSe, ctx.Applicant.PostalCode);
        await FillIfPresentAsync(page, Fields.CitySe, ctx.Applicant.City);

        // Kontaktperson — split name into first + last on a single space
        var (first, last) = SplitName(ctx.ContactName);
        await FillIfPresentAsync(page, Fields.FirstName, first);
        await FillIfPresentAsync(page, Fields.LastName, last);
        await FillIfPresentAsync(page, Fields.Email, ctx.ContactEmail);
        await FillIfPresentAsync(page, Fields.EmailConfirm, ctx.ContactEmail);
        await FillIfPresentAsync(page, Fields.Phone, ctx.ContactPhone);
    }

    private async Task FillStep2Async(IPage page, SubmissionContext ctx, CancellationToken ct)
    {
        // Material checkboxes — Lantmäteriet's form uses radio/checkbox names we
        // haven't pinned to specific __field_ ids in this iteration. Best effort:
        // find checkbox labels matching the material text and click them.
        foreach (var (type, label) in MaterialLabels)
        {
            if (!ctx.MaterialTypes.HasFlag(type)) continue;

            try
            {
                var checkbox = page.Locator($"label:has-text(\"{label}\") input[type='checkbox']");
                if (await checkbox.CountAsync() > 0)
                {
                    await checkbox.First.CheckAsync(new() { Force = true });
                    logger.LogInformation("Checked material option: {Label}", label);
                }
                else
                {
                    logger.LogWarning("Could not locate material checkbox for {Label}", label);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to check material option {Label}", label);
            }
        }
    }

    private async Task FillStep4Async(IPage page, SubmissionContext ctx, CancellationToken ct)
    {
        // Ändamål — large textarea. Lantmäteriet has it labelled "Ändamål" /
        // "Beskriv hur du har för avsikt att använda materialet".
        try
        {
            var purpose = page.Locator("textarea").Filter(new() { HasNotText = "" }).First;
            // Fall back to the first visible textarea on the step
            var visibleTextareas = page.Locator("textarea:visible");
            if (await visibleTextareas.CountAsync() > 0)
            {
                await visibleTextareas.First.FillAsync(ctx.PurposeStatement);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fill purpose statement");
        }

        // Egen referens — usually the last text input on this step
        try
        {
            var inputs = page.Locator("input[type='text']:visible");
            var count = await inputs.CountAsync();
            if (count > 0)
            {
                await inputs.Nth(count - 1).FillAsync(ctx.CaseNumber);
                logger.LogInformation("Filled egen referens with {Ref}", ctx.CaseNumber);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fill own reference");
        }
    }

    private static readonly (DroneMaterialTypes Type, string Label)[] MaterialLabels =
    [
        (DroneMaterialTypes.Snedbilder, "Snedbilder"),
        (DroneMaterialTypes.Lodbilder, "Lodbilder"),
        (DroneMaterialTypes.Video, "Video"),
        (DroneMaterialTypes.Ortofoto, "Ortofoto"),
        (DroneMaterialTypes.OvrigaData, "Övriga data"),
    ];

    private async Task FillIfPresentAsync(IPage page, string fieldName, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        try
        {
            var locator = page.Locator($"[name='{fieldName}']");
            if (await locator.CountAsync() > 0)
            {
                await locator.First.FillAsync(value);
                logger.LogInformation("Filled {Field}", fieldName);
            }
            else
            {
                logger.LogWarning("Field {Field} not found", fieldName);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fill {Field}", fieldName);
        }
    }

    private async Task SelectRadioIfPresentAsync(IPage page, string fieldName, string value)
    {
        try
        {
            var radio = page.Locator($"input[type='radio'][name='{fieldName}'][value='{value}']");
            if (await radio.CountAsync() > 0)
            {
                await radio.First.CheckAsync(new() { Force = true });
                logger.LogInformation("Selected {Field}={Value}", fieldName, value);
            }
            else
            {
                logger.LogWarning("Radio {Field}={Value} not found", fieldName, value);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to select radio {Field}={Value}", fieldName, value);
        }
    }

    private static async Task ClickNextAsync(IPage page, CancellationToken ct)
    {
        var next = page.Locator("button[data-f-navigation-next]:visible");
        if (await next.CountAsync() > 0)
        {
            await next.First.ClickAsync();
        }
    }

    private static async Task TryDismissCookieBannerAsync(IPage page)
    {
        try
        {
            var consent = page.Locator("button:has-text(\"Acceptera\")");
            if (await consent.CountAsync() > 0)
            {
                await consent.First.ClickAsync(new() { Timeout = 3000 });
            }
        }
        catch { /* banner is optional */ }
    }

    private static (string First, string Last) SplitName(string full)
    {
        full = full?.Trim() ?? "";
        if (string.IsNullOrEmpty(full)) return ("", "");
        var space = full.IndexOf(' ');
        return space < 0
            ? (full, "")
            : (full[..space], full[(space + 1)..].Trim());
    }
}
