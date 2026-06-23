using Microsoft.Extensions.Options;
using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Files;

public class LocalCaseStorage(IOptions<LocalStorageOptions> options) : ICaseStorage
{
    public Task<string> CreateCaseFolderAsync(Case c, CancellationToken ct = default)
    {
        var root = options.Value.Root;
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException(
                "Storage:Local:Root is not configured. Set it in appsettings.Local.json.");
        }

        var year = c.CreatedAt.Year.ToString("0000");
        var folderName = $"{c.CaseNumber} - {CaseFolderLayout.SanitizeFolderName(c.Title)}";
        var casePath = Path.Combine(root, year, folderName);

        foreach (var sub in CaseFolderLayout.SubFolders)
        {
            Directory.CreateDirectory(Path.Combine(casePath, sub));
        }

        return Task.FromResult(casePath);
    }
}
