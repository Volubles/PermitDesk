namespace Voluble.PermitDesk.Web.Domain;

public enum CaseStatus
{
    Draft,
    MaterialMissing,
    MaterialReady,
    ApplicationSubmitted,
    WaitingForDecision,
    Approved,
    Rejected,
    ManualReviewRequired
}
