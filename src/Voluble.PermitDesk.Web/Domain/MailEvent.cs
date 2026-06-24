namespace Voluble.PermitDesk.Web.Domain;

public class MailEvent
{
    public Guid Id { get; set; }
    public Guid? CaseId { get; set; }
    public Case? Case { get; set; }

    public required string ExternalMessageId { get; set; }
    public required string From { get; set; }
    public required string Subject { get; set; }
    public string? BodyPreview { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }

    public MailClassification Classification { get; set; } = MailClassification.Unknown;
    public MailMatchConfidence MatchConfidence { get; set; } = MailMatchConfidence.NoMatch;

    public Guid? PdfFileId { get; set; }
    public CaseFile? PdfFile { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }
    public MailProcessingStatus ProcessingStatus { get; set; } = MailProcessingStatus.Pending;

    public string? ProcessingError { get; set; }
}
