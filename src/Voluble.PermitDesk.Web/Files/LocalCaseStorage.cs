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

    public async Task<SavedCaseFile> SaveFileAsync(
        Case caseEntity,
        CaseFileCategory category,
        string fileName,
        Stream content,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(caseEntity.LocalFolderPath))
        {
            throw new InvalidOperationException(
                $"Case {caseEntity.CaseNumber} has no LocalFolderPath; cannot save files.");
        }

        var subFolder = CaseFolderLayout.SubFolderFor(category);
        var targetDir = Path.Combine(caseEntity.LocalFolderPath, subFolder);
        Directory.CreateDirectory(targetDir);

        var safeName = CaseFolderLayout.SanitizeFileName(fileName);
        var targetPath = Path.Combine(targetDir, safeName);

        if (File.Exists(targetPath))
        {
            var nameOnly = Path.GetFileNameWithoutExtension(safeName);
            var ext = Path.GetExtension(safeName);
            var n = 1;
            do
            {
                targetPath = Path.Combine(targetDir, $"{nameOnly} ({n}){ext}");
                n++;
            } while (File.Exists(targetPath));
        }

        await using var fs = File.Create(targetPath);
        await content.CopyToAsync(fs, ct);
        var size = fs.Position;

        return new SavedCaseFile(targetPath, size);
    }

    public Task DeleteFileAsync(CaseFile file, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(file.LocalPath))
            return Task.CompletedTask;

        if (File.Exists(file.LocalPath))
        {
            File.Delete(file.LocalPath);
        }

        return Task.CompletedTask;
    }
}
