using Microsoft.Extensions.Options;
using Voluble.PermitDesk.Web.Domain;
using Voluble.PermitDesk.Web.Files;

namespace Voluble.PermitDesk.Tests;

public class LocalCaseStorageTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly LocalCaseStorage _storage;

    public LocalCaseStorageTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"permitdesk-test-{Guid.NewGuid():N}");
        _storage = new LocalCaseStorage(Options.Create(new LocalStorageOptions { Root = _tempRoot }));
    }

    [Fact]
    public async Task CreateCaseFolderAsync_creates_year_folder_with_five_subfolders()
    {
        var c = NewCase("FBO-2026-0001", "Storgatan 4 Borgholm", year: 2026);

        var path = await _storage.CreateCaseFolderAsync(c);

        var expectedParent = Path.Combine(_tempRoot, "2026", "FBO-2026-0001 - Storgatan 4 Borgholm");
        Assert.Equal(expectedParent, path);
        foreach (var sub in CaseFolderLayout.SubFolders)
        {
            Assert.True(Directory.Exists(Path.Combine(expectedParent, sub)), $"missing subfolder {sub}");
        }
    }

    [Fact]
    public async Task CreateCaseFolderAsync_sanitizes_title_with_invalid_chars()
    {
        var c = NewCase("FBO-2026-0002", "Vägen/Norra\\Södra", year: 2026);

        var path = await _storage.CreateCaseFolderAsync(c);

        Assert.Contains("Vägen Norra Södra", path);
        Assert.True(Directory.Exists(path));
    }

    [Fact]
    public async Task DeleteFileAsync_removes_existing_file_from_disk()
    {
        var c = NewCase("FBO-2026-0010", "Test", year: 2026);
        await _storage.CreateCaseFolderAsync(c);
        c.LocalFolderPath = Path.Combine(_tempRoot, "2026", "FBO-2026-0010 - Test");

        await using (var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 }))
        {
            var saved = await _storage.SaveFileAsync(c, CaseFileCategory.Material, "test.dat", stream);

            var fileRow = new CaseFile
            {
                Id = Guid.NewGuid(),
                FileName = Path.GetFileName(saved.LocalPath),
                FileExtension = ".dat",
                ContentType = "application/octet-stream",
                StorageProvider = "local",
                LocalPath = saved.LocalPath,
                Category = CaseFileCategory.Material,
            };

            Assert.True(File.Exists(saved.LocalPath));
            await _storage.DeleteFileAsync(fileRow);
            Assert.False(File.Exists(saved.LocalPath));
        }
    }

    [Fact]
    public async Task DeleteFileAsync_is_idempotent_when_file_already_missing()
    {
        // Simulates: user removed the file from Explorer before the portal got to it.
        // The DB soft-delete should still succeed; storage layer must not throw.
        var fileRow = new CaseFile
        {
            Id = Guid.NewGuid(),
            FileName = "ghost.dat",
            FileExtension = ".dat",
            ContentType = "application/octet-stream",
            StorageProvider = "local",
            LocalPath = Path.Combine(_tempRoot, "does", "not", "exist.dat"),
            Category = CaseFileCategory.Material,
        };

        await _storage.DeleteFileAsync(fileRow);
    }

    [Fact]
    public async Task DeleteFileAsync_skips_silently_when_LocalPath_is_blank()
    {
        var fileRow = new CaseFile
        {
            Id = Guid.NewGuid(),
            FileName = "nopath.dat",
            FileExtension = ".dat",
            ContentType = "application/octet-stream",
            StorageProvider = "local",
            LocalPath = null,
            Category = CaseFileCategory.Material,
        };

        await _storage.DeleteFileAsync(fileRow);
    }

    [Fact]
    public async Task CreateCaseFolderAsync_throws_when_root_is_unconfigured()
    {
        var storage = new LocalCaseStorage(Options.Create(new LocalStorageOptions { Root = "" }));
        var c = NewCase("FBO-2026-0003", "Anything", year: 2026);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => storage.CreateCaseFolderAsync(c));
    }

    private static Case NewCase(string caseNumber, string title, int year)
        => new()
        {
            Id = Guid.NewGuid(),
            CaseNumber = caseNumber,
            Title = title,
            Address = "irrelevant",
            Municipality = "irrelevant",
            CreatedAt = new DateTimeOffset(year, 6, 15, 12, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(year, 6, 15, 12, 0, 0, TimeSpan.Zero),
        };

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }
}
