using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Files;

public interface ICaseStorage
{
    Task<string> CreateCaseFolderAsync(Case caseEntity, CancellationToken ct = default);
}
