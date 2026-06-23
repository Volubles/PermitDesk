using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Files;

public record SavedCaseFile(string LocalPath, long SizeBytes);

public interface ICaseStorage
{
    Task<string> CreateCaseFolderAsync(Case caseEntity, CancellationToken ct = default);

    Task<SavedCaseFile> SaveFileAsync(
        Case caseEntity,
        CaseFileCategory category,
        string fileName,
        Stream content,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a file from underlying storage. Idempotent — a missing file is
    /// not an error so that disk-side deletions don't block the DB soft-delete.
    /// </summary>
    Task DeleteFileAsync(CaseFile file, CancellationToken ct = default);
}
