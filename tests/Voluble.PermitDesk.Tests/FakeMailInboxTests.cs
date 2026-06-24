using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Voluble.PermitDesk.Web.Mail;

namespace Voluble.PermitDesk.Tests;

public class FakeMailInboxTests : IDisposable
{
    private readonly string _tempDir;

    public FakeMailInboxTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"permitdesk-mail-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task FetchAllAsync_parses_eml_files_in_folder()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "one.eml"), Eml(
            messageId: "<m1@lantmateriet.se>",
            subject: "Bekräftelse FBO-2026-0001",
            body: "Vi har mottagit din ansökan med ref FBO-2026-0001."));
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "two.eml"), Eml(
            messageId: "<m2@lantmateriet.se>",
            subject: "Beslut FBO-2026-0002",
            body: "Beviljat."));

        var inbox = NewInbox();
        var results = await ToListAsync(inbox.FetchAllAsync());

        Assert.Equal(2, results.Count);
        Assert.Contains(results, m => m.MessageId == "m1@lantmateriet.se" && m.Subject.Contains("FBO-2026-0001"));
        Assert.Contains(results, m => m.MessageId == "m2@lantmateriet.se" && m.Subject.Contains("FBO-2026-0002"));
    }

    [Fact]
    public async Task FetchAllAsync_returns_empty_when_folder_missing()
    {
        var inbox = new FakeMailInbox(
            Options.Create(new FakeMailInboxOptions { Path = Path.Combine(_tempDir, "does-not-exist") }),
            NullLogger<FakeMailInbox>.Instance);

        var results = await ToListAsync(inbox.FetchAllAsync());

        Assert.Empty(results);
    }

    [Fact]
    public async Task FetchAllAsync_skips_unparseable_files()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "garbage.eml"), "This is not a valid mail.");
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "good.eml"), Eml(
            messageId: "<good@test>",
            subject: "Ok",
            body: "Body"));

        var inbox = NewInbox();
        var results = await ToListAsync(inbox.FetchAllAsync());

        Assert.Single(results);
        Assert.Equal("good@test", results[0].MessageId);
    }

    [Fact]
    public async Task FetchAllAsync_synthesises_message_id_when_missing()
    {
        // Mail without a Message-ID header — fall back to filename-based id so dedup still works.
        var emlWithoutId = """
            From: someone@example.se
            To: receiver@example.se
            Subject: Inget message id

            kropp
            """;
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "no-id.eml"), emlWithoutId);

        var inbox = NewInbox();
        var results = await ToListAsync(inbox.FetchAllAsync());

        Assert.Single(results);
        Assert.StartsWith("file:", results[0].MessageId);
    }

    private FakeMailInbox NewInbox()
        => new(
            Options.Create(new FakeMailInboxOptions { Path = _tempDir }),
            NullLogger<FakeMailInbox>.Instance);

    private static string Eml(string messageId, string subject, string body) => $"""
        From: spridningstillstand@lantmateriet.se
        To: spridningstillstand.oland@fastighetsbyran.se
        Subject: {subject}
        Date: Mon, 8 Jun 2026 09:14:22 +0200
        Message-ID: {messageId}
        MIME-Version: 1.0
        Content-Type: text/plain; charset=utf-8

        {body}
        """;

    private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source) list.Add(item);
        return list;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
