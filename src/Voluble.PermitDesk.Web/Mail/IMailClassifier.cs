using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Mail;

public interface IMailClassifier
{
    MailClassification Classify(InboundMail mail);
}
