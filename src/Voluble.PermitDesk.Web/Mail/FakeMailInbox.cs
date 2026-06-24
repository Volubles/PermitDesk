using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Voluble.PermitDesk.Web.Mail;

public class FakeMailInbox(
    IOptions<FakeMailInboxOptions> options,
    ILogger<FakeMailInbox> logger) : IMailInbox
{
    public async IAsyncEnumerable<InboundMail> FetchAllAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var path = options.Value.Path;
        if (string.IsNullOrWhiteSpace(path))
        {
            logger.LogWarning("Mail:FakeInbox:Path is not configured; inbox returns 0 messages.");
            yield break;
        }

        if (!Directory.Exists(path))
        {
            logger.LogWarning("Fake mail inbox folder {Path} does not exist; inbox returns 0 messages.", path);
            yield break;
        }

        var emlFiles = Directory.EnumerateFiles(path, "*.eml", SearchOption.TopDirectoryOnly);

        foreach (var file in emlFiles)
        {
            ct.ThrowIfCancellationRequested();

            InboundMail? parsed;
            try
            {
                parsed = await ParseAsync(file, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse {File}; skipping.", file);
                continue;
            }

            if (parsed is not null)
                yield return parsed;
        }
    }

    private static async Task<InboundMail?> ParseAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var message = await MimeMessage.LoadAsync(stream, ct);

        var messageId = string.IsNullOrWhiteSpace(message.MessageId)
            ? $"file:{Path.GetFileName(path)}"
            : message.MessageId;

        var from = message.From.Mailboxes.FirstOrDefault()?.Address
                   ?? message.From.ToString();

        var subject = message.Subject ?? string.Empty;
        var htmlBody = message.HtmlBody;
        // Lantmäteriet's real mails are HTML-only. Convert to text so the
        // classifier and extractors don't have to substring-match against raw
        // markup (and the rendered PDF stays readable).
        var textBody = message.TextBody
            ?? (htmlBody is not null ? HtmlToText.Convert(htmlBody) : string.Empty);
        // Normalise to UTC. Postgres timestamptz only accepts offset 0, and we
        // want the rest of the pipeline (PDF render, DB persist, display) to
        // work off a single timezone — UTC stored, .ToLocalDateTime() at the
        // display boundary.
        var receivedRaw = message.Date == DateTimeOffset.MinValue
            ? new DateTimeOffset(File.GetCreationTimeUtc(path), TimeSpan.Zero)
            : message.Date;
        var receivedAt = receivedRaw.ToUniversalTime();

        return new InboundMail(messageId, from, subject, textBody, htmlBody, receivedAt, path);
    }
}
