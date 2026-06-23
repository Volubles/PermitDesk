using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Cases;

public static class PublicationEligibility
{
    public static PublicationStatus Evaluate(Case c, IReadOnlyCollection<CaseFile> files)
    {
        if (c.Status == CaseStatus.ManualReviewRequired)
            return PublicationStatus.Unknown;

        if (c.Status == CaseStatus.Approved)
        {
            return files.Any(f => f.Category == CaseFileCategory.Decision)
                ? PublicationStatus.Allowed
                : PublicationStatus.Unknown;
        }

        return PublicationStatus.NotAllowed;
    }
}
