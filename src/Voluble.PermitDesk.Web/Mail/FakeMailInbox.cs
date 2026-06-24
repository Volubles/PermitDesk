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
        var textBody = message.TextBody ?? message.HtmlBody ?? string.Empty;
        var htmlBody = message.HtmlBody;
        var receivedAt = message.Date == DateTimeOffset.MinValue
            ? File.GetCreationTimeUtc(path)
            : message.Date;

        return new InboundMail(messageId, from, subject, textBody, htmlBody, receivedAt, path);
    }
}
