namespace Voluble.PermitDesk.Web.Mail;

public record MailProcessingSummary(int Processed, int SkippedDuplicates, int ManualReview, int Failed);

public interface IMailProcessor
{
    Task<MailProcessingSummary> ProcessAllAsync(CancellationToken ct = default);
}
