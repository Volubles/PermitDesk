namespace Voluble.PermitDesk.Web.Domain;

public enum MailProcessingStatus
{
    Pending,
    Processed,
    FailedClassification,
    FailedMatching,
    FailedPdf,
    ManualReviewRequired,
}
