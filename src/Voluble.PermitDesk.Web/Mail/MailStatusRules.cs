using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Mail;

/// <summary>
/// Pure mapping from (current case status × incoming mail classification) to
/// the case status that should result. Decision mails are terminal — they
/// override whatever state the case is in. Earlier confirmations are guarded
/// so we don't accidentally move a case backwards.
/// </summary>
public static class MailStatusRules
{
    public static CaseStatus NextStatusFor(CaseStatus current, MailClassification classification)
    {
        return classification switch
        {
            MailClassification.DecisionApproved => CaseStatus.Approved,
            MailClassification.DecisionRejected => CaseStatus.Rejected,
            MailClassification.DecisionNeedsAction => CaseStatus.ManualReviewRequired,

            MailClassification.SubmissionConfirmation when IsAtOrBefore(current, CaseStatus.ApplicationSubmitted)
                => CaseStatus.WaitingForDecision,

            MailClassification.UploadLinkReceived when IsAtOrBefore(current, CaseStatus.ApplicationSubmitted)
                => CaseStatus.ApplicationSubmitted,

            MailClassification.ApplicationConfirmation when IsAtOrBefore(current, CaseStatus.MaterialReady)
                => CaseStatus.ApplicationSubmitted,

            _ => current,
        };
    }

    public static CaseFileCategory CategoryFor(MailClassification classification) => classification switch
    {
        MailClassification.ApplicationConfirmation => CaseFileCategory.Application,
        MailClassification.UploadLinkReceived => CaseFileCategory.Application,
        MailClassification.SubmissionConfirmation => CaseFileCategory.SubmissionConfirmation,
        MailClassification.DecisionApproved => CaseFileCategory.Decision,
        MailClassification.DecisionRejected => CaseFileCategory.Decision,
        MailClassification.DecisionNeedsAction => CaseFileCategory.Other,
        _ => CaseFileCategory.Other,
    };

    private static bool IsAtOrBefore(CaseStatus current, CaseStatus reference)
    {
        // Status enum is intentionally declared in workflow order — this is the only
        // place that relies on it. If the order changes, this comparison needs to too.
        return (int)current <= (int)reference;
    }
}
