namespace Voluble.PermitDesk.Web.Mail;

public interface IMailPdfRenderer
{
    /// <summary>
    /// Renders a single mail to a self-contained PDF byte stream.
    /// Output is meant for archival, not pretty layout — it's the evidence file
    /// that lives in 03_Bekraftelser or 04_Beslut for the case.
    /// </summary>
    Task<byte[]> RenderAsync(InboundMail mail, CancellationToken ct = default);
}
