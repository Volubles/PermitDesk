using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Lantmateriet;

public enum SubmissionStep
{
    Starting,
    LaunchingBrowser,
    FillingForm,
    AwaitingMap,
    AwaitingConfirmation,
    Submitting,
    Completed,
    Cancelled,
    Failed,
}

/// <summary>
/// Per-case state for an in-flight Playwright submission. The submitter pushes
/// status changes through SetStep; the Submit.razor page subscribes via
/// StateChanged for re-rendering. The TCS gates the final click on Skicka so
/// the broker has to explicitly confirm in PermitDesk's UI before anything
/// hits Lantmäteriet.
/// </summary>
public class SubmissionSession
{
    public Guid CaseId { get; }
    public SubmissionStep Step { get; private set; } = SubmissionStep.Starting;
    public string? StatusMessage { get; private set; }
    public string? ErrorMessage { get; private set; }
    public byte[]? ConfirmationScreenshot { get; private set; }
    public Lantmateriet.SubmissionContext Context { get; }

    public event Action? StateChanged;

    private readonly TaskCompletionSource<bool> _confirmTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly CancellationTokenSource _cts = new();

    public SubmissionSession(Guid caseId, Lantmateriet.SubmissionContext context)
    {
        CaseId = caseId;
        Context = context;
    }

    public CancellationToken CancellationToken => _cts.Token;

    public void SetStep(SubmissionStep step, string? message = null)
    {
        Step = step;
        StatusMessage = message;
        StateChanged?.Invoke();
    }

    public void Fail(string message)
    {
        Step = SubmissionStep.Failed;
        ErrorMessage = message;
        _confirmTcs.TrySetResult(false);
        StateChanged?.Invoke();
    }

    public void Cancel()
    {
        Step = SubmissionStep.Cancelled;
        _confirmTcs.TrySetResult(false);
        _cts.Cancel();
        StateChanged?.Invoke();
    }

    public void Confirm() => _confirmTcs.TrySetResult(true);

    public Task<bool> WaitForConfirmationAsync() => _confirmTcs.Task;

    public void SetScreenshot(byte[] bytes)
    {
        ConfirmationScreenshot = bytes;
        StateChanged?.Invoke();
    }
}

/// <summary>
/// Bag of values the submitter pulls from. Built once on session start so the
/// submitter doesn't need IDbContextFactory and the Submit.razor page can
/// preview what would be sent before launching the browser.
/// </summary>
public record SubmissionContext(
    Guid CaseId,
    string CaseNumber,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    DroneMaterialTypes MaterialTypes,
    string PurposeStatement,
    ApplicantOptions Applicant);
