namespace Voluble.PermitDesk.Web.Domain;

public enum MailClassification
{
    Unknown,
    ApplicationConfirmation,
    UploadLinkReceived,
    SubmissionConfirmation,
    DecisionApproved,
    DecisionRejected,
    DecisionNeedsAction,
}
