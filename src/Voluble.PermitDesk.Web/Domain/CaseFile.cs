namespace Voluble.PermitDesk.Web.Domain;

public class CaseFile
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public Case Case { get; set; } = null!;

    public required string FileName { get; set; }
    public required string FileExtension { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }

    public CaseFileCategory Category { get; set; }

    public required string StorageProvider { get; set; }
    public string? StorageItemId { get; set; }
    public string? StorageWebUrl { get; set; }
    public string? LocalPath { get; set; }

    public string? UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
