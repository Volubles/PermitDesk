using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Mail;

/// <summary>
/// Order-sensitive rule list. The first matching rule wins, so the most specific
/// signals (decision verbs) must come before the most generic ones (a bare
/// "bekräftelse" subject could be either an application confirmation or a
/// submission confirmation).
/// </summary>
public class RuleBasedMailClassifier : IMailClassifier
{
    private static readonly Rule[] Rules =
    [
        new(MailClassification.DecisionApproved,
            SubjectAny: ["beviljat", "godkänt"],
            BodyAny:    ["beviljar", "har beslutat att bevilja", "spridningstillstånd beviljas"]),

        new(MailClassification.DecisionRejected,
            SubjectAny: ["avslå", "avslag", "avslås"],
            BodyAny:    ["avslå", "avslås", "avslår", "avslag på"]),

        new(MailClassification.DecisionNeedsAction,
            SubjectAny: ["komplettering", "behöver komplettering"],
            BodyAny:    ["komplettering", "vänligen komplettera"]),

        new(MailClassification.UploadLinkReceived,
            SubjectAny: ["ladda upp", "uppladdning", "ladda upp material"],
            BodyAny:    ["uppladdningslänk", "länk för att ladda upp"]),

        new(MailClassification.SubmissionConfirmation,
            SubjectAny: ["material mottaget", "mottaget material"],
            BodyAny:    ["mottagit material för granskning", "material för granskning"]),

        new(MailClassification.ApplicationConfirmation,
            SubjectAny: ["bekräftelse", "mottagen ansökan", "ansökan mottagen", "registrerat er ansökan"],
            BodyAny:    ["mottagit din ansökan", "tack för din ansökan", "registrerat er ansökan"]),
    ];

    public MailClassification Classify(InboundMail mail)
    {
        var subject = mail.Subject ?? string.Empty;
        var body = mail.TextBody ?? string.Empty;

        foreach (var rule in Rules)
        {
            if (Matches(subject, rule.SubjectAny) || Matches(body, rule.BodyAny))
                return rule.Classification;
        }

        return MailClassification.Unknown;
    }

    private static bool Matches(string text, string[] needles)
    {
        foreach (var n in needles)
        {
            if (text.Contains(n, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private record Rule(MailClassification Classification, string[] SubjectAny, string[] BodyAny);
}
