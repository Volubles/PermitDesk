using Voluble.PermitDesk.Web.Domain;
using Voluble.PermitDesk.Web.Mail;

namespace Voluble.PermitDesk.Tests;

public class RuleBasedMailClassifierTests
{
    private readonly RuleBasedMailClassifier _classifier = new();

    [Theory]
    [InlineData("Bekräftelse på mottagen ansökan om spridningstillstånd",
                "Vi har mottagit din ansökan med din egen referens FBO-2026-0001.",
                MailClassification.ApplicationConfirmation)]
    [InlineData("Ladda upp material för ärende FBO-2026-0001",
                "Använd länken nedan för att ladda upp materialet.",
                MailClassification.UploadLinkReceived)]
    [InlineData("Material mottaget - FBO-2026-0001",
                "Vi har mottagit material för granskning.",
                MailClassification.SubmissionConfirmation)]
    [InlineData("Beslut: Spridningstillstånd beviljat - FBO-2026-0001",
                "Lantmäteriet har beslutat att bevilja spridningstillstånd.",
                MailClassification.DecisionApproved)]
    [InlineData("Beslut: Spridningstillstånd avslås - FBO-2026-0009",
                "Lantmäteriet har beslutat att avslå ansökan.",
                MailClassification.DecisionRejected)]
    [InlineData("Komplettering önskas",
                "Vi behöver komplettering av det inlämnade materialet.",
                MailClassification.DecisionNeedsAction)]
    [InlineData("Oklart ämne", "Helt orelaterad text.", MailClassification.Unknown)]
    public void Classifies_mail_by_subject_and_body(string subject, string body, MailClassification expected)
    {
        var mail = NewMail(subject, body);
        Assert.Equal(expected, _classifier.Classify(mail));
    }

    [Fact]
    public void Decision_wording_in_body_beats_generic_bekraftelse_in_subject()
    {
        // The classifier MUST prefer the more specific signal. A subject like
        // "Bekräftelse" combined with body that says "vi har beslutat att bevilja"
        // is a Decision, not an ApplicationConfirmation.
        var mail = NewMail(
            subject: "Bekräftelse",
            body: "Lantmäteriet har beslutat att bevilja spridningstillstånd.");

        Assert.Equal(MailClassification.DecisionApproved, _classifier.Classify(mail));
    }

    [Fact]
    public void Case_insensitive_match()
    {
        var mail = NewMail(
            subject: "BEKRÄFTELSE PÅ MOTTAGEN ANSÖKAN",
            body: "VI HAR MOTTAGIT DIN ANSÖKAN.");

        Assert.Equal(MailClassification.ApplicationConfirmation, _classifier.Classify(mail));
    }

    [Fact]
    public void Empty_or_null_fields_classify_as_Unknown()
    {
        var mail = new InboundMail(
            MessageId: "x@x",
            From: "x@x",
            Subject: "",
            TextBody: "",
            HtmlBody: null,
            ReceivedAt: DateTimeOffset.UtcNow,
            SourcePath: "/dev/null");

        Assert.Equal(MailClassification.Unknown, _classifier.Classify(mail));
    }

    private static InboundMail NewMail(string subject, string body)
        => new(
            MessageId: $"<test-{Guid.NewGuid():N}@example>",
            From: "spridningstillstand@lantmateriet.se",
            Subject: subject,
            TextBody: body,
            HtmlBody: null,
            ReceivedAt: DateTimeOffset.UtcNow,
            SourcePath: "/test");
}
