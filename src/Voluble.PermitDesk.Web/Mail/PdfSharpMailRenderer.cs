using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Snippets.Font;

namespace Voluble.PermitDesk.Web.Mail;

public class PdfSharpMailRenderer : IMailPdfRenderer
{
    static PdfSharpMailRenderer()
    {
        // PdfSharp 6.x needs an explicit font resolver. FailsafeFontResolver
        // ships with PdfSharp and falls back to bundled fonts when the OS
        // doesn't have a match — works the same on Windows, Linux, and macOS.
        if (GlobalFontSettings.FontResolver is null)
        {
            GlobalFontSettings.FontResolver = new FailsafeFontResolver();
        }
    }

    public Task<byte[]> RenderAsync(InboundMail mail, CancellationToken ct = default)
    {
        using var document = new PdfDocument();
        document.Info.Title = Truncate(mail.Subject, 200);
        document.Info.Author = mail.From;
        document.Info.Subject = "Mail snapshot from PermitDesk";
        document.Info.Creator = "PermitDesk";

        var headerFont = new XFont("Arial", 16, XFontStyleEx.Bold);
        var labelFont = new XFont("Arial", 9, XFontStyleEx.Bold);
        var metaFont = new XFont("Arial", 9, XFontStyleEx.Regular);
        var bodyFont = new XFont("Arial", 10, XFontStyleEx.Regular);
        var footerFont = new XFont("Arial", 7, XFontStyleEx.Italic);

        const double pageMargin = 40;
        const double labelWidth = 75;

        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var pageWidth = page.Width.Point;
        var pageHeight = page.Height.Point;
        var contentWidth = pageWidth - 2 * pageMargin;
        var y = pageMargin;

        // Subject
        y = DrawWrapped(gfx, mail.Subject ?? "(utan ämne)", headerFont, XBrushes.Black,
            pageMargin, y, contentWidth);
        y += 8;

        // Divider
        gfx.DrawLine(XPens.LightGray, pageMargin, y, pageWidth - pageMargin, y);
        y += 10;

        // Metadata block
        y = DrawMeta(gfx, "Från:", mail.From, labelFont, metaFont, pageMargin, y, labelWidth, contentWidth - labelWidth);
        y = DrawMeta(gfx, "Datum:", mail.ReceivedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss"), labelFont, metaFont, pageMargin, y, labelWidth, contentWidth - labelWidth);
        y = DrawMeta(gfx, "Message-ID:", mail.MessageId, labelFont, metaFont, pageMargin, y, labelWidth, contentWidth - labelWidth);
        y += 10;

        gfx.DrawLine(XPens.LightGray, pageMargin, y, pageWidth - pageMargin, y);
        y += 10;

        // Body — wrap & paginate
        var body = mail.TextBody ?? string.Empty;
        foreach (var paragraph in body.Replace("\r\n", "\n").Split('\n'))
        {
            if (y > pageHeight - pageMargin - 30)
            {
                gfx.Dispose();
                page = document.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                y = pageMargin;
            }

            if (string.IsNullOrEmpty(paragraph))
            {
                y += 6;
                continue;
            }

            y = DrawWrapped(gfx, paragraph, bodyFont, XBrushes.Black, pageMargin, y, contentWidth);
            y += 2;
        }

        // Footer on the last page
        gfx.DrawString(
            $"Sparat av PermitDesk {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC",
            footerFont, XBrushes.Gray,
            new XRect(pageMargin, pageHeight - pageMargin, contentWidth, 12),
            XStringFormats.BottomLeft);

        gfx.Dispose();

        using var stream = new MemoryStream();
        document.Save(stream);
        return Task.FromResult(stream.ToArray());
    }

    private static double DrawMeta(XGraphics gfx, string label, string value, XFont labelFont, XFont valueFont,
        double x, double y, double labelWidth, double valueWidth)
    {
        gfx.DrawString(label, labelFont, XBrushes.Black, new XPoint(x, y + 12));
        return DrawWrapped(gfx, value, valueFont, XBrushes.Black, x + labelWidth, y, valueWidth);
    }

    private static double DrawWrapped(XGraphics gfx, string text, XFont font, XBrush brush,
        double x, double y, double maxWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return y + font.Height;
        }

        var words = text.Split(' ');
        var line = new System.Text.StringBuilder();

        foreach (var word in words)
        {
            var candidate = line.Length == 0 ? word : line + " " + word;
            var size = gfx.MeasureString(candidate, font);
            if (size.Width > maxWidth && line.Length > 0)
            {
                gfx.DrawString(line.ToString(), font, brush, new XPoint(x, y + font.Height));
                y += font.Height;
                line.Clear();
                line.Append(word);
            }
            else
            {
                line.Clear();
                line.Append(candidate);
            }
        }

        if (line.Length > 0)
        {
            gfx.DrawString(line.ToString(), font, brush, new XPoint(x, y + font.Height));
            y += font.Height;
        }

        return y;
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
