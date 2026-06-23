namespace Voluble.PermitDesk.Web.Files;

public static class CaseFolderLayout
{
    public const string Material = "01_Material";
    public const string Application = "02_Ansokan";
    public const string Confirmations = "03_Bekraftelser";
    public const string Decisions = "04_Beslut";
    public const string Other = "05_Ovrigt";

    public static readonly IReadOnlyList<string> SubFolders =
    [
        Material,
        Application,
        Confirmations,
        Decisions,
        Other,
    ];

    public static string SubFolderFor(Domain.CaseFileCategory category) => category switch
    {
        Domain.CaseFileCategory.Material => Material,
        Domain.CaseFileCategory.Application => Application,
        Domain.CaseFileCategory.SubmissionConfirmation => Confirmations,
        Domain.CaseFileCategory.Decision => Decisions,
        Domain.CaseFileCategory.Other => Other,
        _ => Other,
    };

    public static string SanitizeFolderName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "Untitled";

        var invalid = Path.GetInvalidFileNameChars();
        var chars = new char[input.Length];
        var i = 0;
        var lastWasSpace = false;

        foreach (var c in input)
        {
            if (invalid.Contains(c) || c == '\r' || c == '\n' || c == '\t')
            {
                if (!lastWasSpace)
                {
                    chars[i++] = ' ';
                    lastWasSpace = true;
                }
            }
            else if (c == ' ')
            {
                if (!lastWasSpace)
                {
                    chars[i++] = c;
                    lastWasSpace = true;
                }
            }
            else
            {
                chars[i++] = c;
                lastWasSpace = false;
            }
        }

        var sanitized = new string(chars, 0, i).Trim().TrimEnd('.');
        return string.IsNullOrEmpty(sanitized) ? "Untitled" : sanitized;
    }
}
