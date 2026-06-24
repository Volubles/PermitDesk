namespace Voluble.PermitDesk.Web.Mail;

public interface IMailInbox
{
    /// <summary>
    /// Returns all mail currently sitting in the inbox. Deduplication against
    /// already-processed mail happens at the DB layer via MailEvent.ExternalMessageId.
    /// </summary>
    IAsyncEnumerable<InboundMail> FetchAllAsync(CancellationToken ct = default);
}
