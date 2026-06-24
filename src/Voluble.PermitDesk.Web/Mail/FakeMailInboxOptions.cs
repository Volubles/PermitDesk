namespace Voluble.PermitDesk.Web.Mail;

public class FakeMailInboxOptions
{
    public const string SectionName = "Mail:FakeInbox";

    /// <summary>
    /// Absolute path to a folder containing .eml files. Each file is treated
    /// as one inbound mail. Missing folder is logged and treated as empty.
    /// </summary>
    public string Path { get; set; } = string.Empty;
}
