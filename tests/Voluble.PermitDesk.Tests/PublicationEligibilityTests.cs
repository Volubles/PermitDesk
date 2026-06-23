using Voluble.PermitDesk.Web.Cases;
using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Tests;

public class PublicationEligibilityTests
{
    [Fact]
    public void Approved_with_decision_file_is_Allowed()
    {
        var c = CaseWith(CaseStatus.Approved);
        var files = new[] { FileOf(CaseFileCategory.Decision) };

        Assert.Equal(PublicationStatus.Allowed, PublicationEligibility.Evaluate(c, files));
    }

    [Fact]
    public void Approved_without_decision_file_is_Unknown()
    {
        var c = CaseWith(CaseStatus.Approved);
        var files = new[]
        {
            FileOf(CaseFileCategory.Material),
            FileOf(CaseFileCategory.SubmissionConfirmation),
        };

        Assert.Equal(PublicationStatus.Unknown, PublicationEligibility.Evaluate(c, files));
    }

    [Fact]
    public void Approved_with_empty_files_is_Unknown()
    {
        var c = CaseWith(CaseStatus.Approved);

        Assert.Equal(PublicationStatus.Unknown, PublicationEligibility.Evaluate(c, Array.Empty<CaseFile>()));
    }

    [Theory]
    [InlineData(CaseStatus.Draft)]
    [InlineData(CaseStatus.MaterialMissing)]
    [InlineData(CaseStatus.MaterialReady)]
    [InlineData(CaseStatus.ApplicationSubmitted)]
    [InlineData(CaseStatus.WaitingForDecision)]
    [InlineData(CaseStatus.Rejected)]
    public void Pre_approval_and_rejected_are_NotAllowed(CaseStatus status)
    {
        var c = CaseWith(status);
        var files = new[] { FileOf(CaseFileCategory.Decision) };

        Assert.Equal(PublicationStatus.NotAllowed, PublicationEligibility.Evaluate(c, files));
    }

    [Fact]
    public void ManualReviewRequired_is_Unknown_regardless_of_files()
    {
        var c = CaseWith(CaseStatus.ManualReviewRequired);

        Assert.Equal(PublicationStatus.Unknown,
            PublicationEligibility.Evaluate(c, [FileOf(CaseFileCategory.Decision)]));
        Assert.Equal(PublicationStatus.Unknown,
            PublicationEligibility.Evaluate(c, []));
    }

    private static Case CaseWith(CaseStatus status) => new()
    {
        Id = Guid.NewGuid(),
        CaseNumber = "FBO-2026-0001",
        Title = "irrelevant",
        Address = "irrelevant",
        Municipality = "irrelevant",
        Status = status,
    };

    private static CaseFile FileOf(CaseFileCategory category) => new()
    {
        Id = Guid.NewGuid(),
        FileName = "x",
        FileExtension = ".x",
        ContentType = "application/octet-stream",
        StorageProvider = "local",
        Category = category,
    };
}
