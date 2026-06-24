using Voluble.PermitDesk.Web.Domain;
using Voluble.PermitDesk.Web.Mail;

namespace Voluble.PermitDesk.Tests;

public class MailStatusRulesTests
{
    [Theory]
    [InlineData(CaseStatus.Draft, MailClassification.DecisionApproved, CaseStatus.Approved)]
    [InlineData(CaseStatus.WaitingForDecision, MailClassification.DecisionApproved, CaseStatus.Approved)]
    [InlineData(CaseStatus.Rejected, MailClassification.DecisionApproved, CaseStatus.Approved)]
    public void DecisionApproved_always_moves_to_Approved(
        CaseStatus current, MailClassification classification, CaseStatus expected)
    {
        Assert.Equal(expected, MailStatusRules.NextStatusFor(current, classification));
    }

    [Theory]
    [InlineData(CaseStatus.WaitingForDecision)]
    [InlineData(CaseStatus.Approved)]
    public void DecisionRejected_moves_to_Rejected(CaseStatus current)
    {
        Assert.Equal(CaseStatus.Rejected,
            MailStatusRules.NextStatusFor(current, MailClassification.DecisionRejected));
    }

    [Fact]
    public void DecisionNeedsAction_moves_to_ManualReview()
    {
        Assert.Equal(CaseStatus.ManualReviewRequired,
            MailStatusRules.NextStatusFor(CaseStatus.WaitingForDecision, MailClassification.DecisionNeedsAction));
    }

    [Theory]
    [InlineData(CaseStatus.Draft, CaseStatus.WaitingForDecision)]
    [InlineData(CaseStatus.MaterialReady, CaseStatus.WaitingForDecision)]
    [InlineData(CaseStatus.ApplicationSubmitted, CaseStatus.WaitingForDecision)]
    public void SubmissionConfirmation_moves_forward_to_WaitingForDecision(
        CaseStatus current, CaseStatus expected)
    {
        Assert.Equal(expected,
            MailStatusRules.NextStatusFor(current, MailClassification.SubmissionConfirmation));
    }

    [Fact]
    public void SubmissionConfirmation_does_not_move_backwards_from_Approved()
    {
        // Late-arriving confirmation mail must not unflip a closed case.
        Assert.Equal(CaseStatus.Approved,
            MailStatusRules.NextStatusFor(CaseStatus.Approved, MailClassification.SubmissionConfirmation));
    }

    [Fact]
    public void ApplicationConfirmation_does_not_move_backwards_from_WaitingForDecision()
    {
        Assert.Equal(CaseStatus.WaitingForDecision,
            MailStatusRules.NextStatusFor(CaseStatus.WaitingForDecision, MailClassification.ApplicationConfirmation));
    }

    [Fact]
    public void UnknownClassification_is_noop()
    {
        Assert.Equal(CaseStatus.MaterialReady,
            MailStatusRules.NextStatusFor(CaseStatus.MaterialReady, MailClassification.Unknown));
    }

    [Theory]
    [InlineData(MailClassification.ApplicationConfirmation, CaseFileCategory.Application)]
    [InlineData(MailClassification.UploadLinkReceived, CaseFileCategory.Application)]
    [InlineData(MailClassification.SubmissionConfirmation, CaseFileCategory.SubmissionConfirmation)]
    [InlineData(MailClassification.DecisionApproved, CaseFileCategory.Decision)]
    [InlineData(MailClassification.DecisionRejected, CaseFileCategory.Decision)]
    [InlineData(MailClassification.DecisionNeedsAction, CaseFileCategory.Other)]
    [InlineData(MailClassification.Unknown, CaseFileCategory.Other)]
    public void CategoryFor_maps_classification_to_case_file_category(
        MailClassification classification, CaseFileCategory expected)
    {
        Assert.Equal(expected, MailStatusRules.CategoryFor(classification));
    }
}
