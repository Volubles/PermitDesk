using Microsoft.EntityFrameworkCore;
using Voluble.PermitDesk.Web.Cases;
using Voluble.PermitDesk.Web.Data;
using Voluble.PermitDesk.Web.Domain;
using Voluble.PermitDesk.Web.Files;

namespace Voluble.PermitDesk.Web.Mail;

public class MailProcessor(
    IDbContextFactory<PermitDeskDbContext> dbFactory,
    IMailInbox inbox,
    IMailClassifier classifier,
    ICaseMatcher matcher,
    IMailPdfRenderer pdfRenderer,
    ICaseStorage storage,
    ILogger<MailProcessor> logger) : IMailProcessor
{
    public async Task<MailProcessingSummary> ProcessAllAsync(CancellationToken ct = default)
    {
        var processed = 0;
        var skipped = 0;
        var manualReview = 0;
        var failed = 0;

        await foreach (var mail in inbox.FetchAllAsync(ct))
        {
            switch (await TryProcessOneAsync(mail, ct))
            {
                case Outcome.Processed: processed++; break;
                case Outcome.AlreadyProcessed: skipped++; break;
                case Outcome.ManualReview: manualReview++; break;
                case Outcome.Failed: failed++; break;
            }
        }

        var summary = new MailProcessingSummary(processed, skipped, manualReview, failed);
        logger.LogInformation("Mail processing summary: {Summary}", summary);
        return summary;
    }

    private async Task<Outcome> TryProcessOneAsync(InboundMail mail, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Dedup — the unique index on ExternalMessageId is the real defense, this is
        // just to skip the work cleanly when we already know.
        var alreadySeen = await db.MailEvents
            .AnyAsync(m => m.ExternalMessageId == mail.MessageId, ct);
        if (alreadySeen) return Outcome.AlreadyProcessed;

        var classification = classifier.Classify(mail);
        var match = await matcher.MatchAsync(mail, ct);

        // Route to manual review if we can't be confident
        var needsReview =
            match.Confidence == MailMatchConfidence.NoMatch ||
            classification == MailClassification.Unknown;

        if (needsReview)
        {
            db.MailEvents.Add(NewEvent(mail, classification, match,
                processingStatus: MailProcessingStatus.ManualReviewRequired));
            await db.SaveChangesAsync(ct);
            return Outcome.ManualReview;
        }

        try
        {
            var caseEntity = await db.Cases.FirstAsync(c => c.Id == match.CaseId!.Value, ct);

            // Render + save the PDF
            var pdfBytes = await pdfRenderer.RenderAsync(mail, ct);
            var category = MailStatusRules.CategoryFor(classification);
            var fileName = BuildPdfName(classification, mail.ReceivedAt);

            await using var pdfStream = new MemoryStream(pdfBytes);
            var saved = await storage.SaveFileAsync(caseEntity, category, fileName, pdfStream, ct);

            var pdfFile = new CaseFile
            {
                Id = Guid.NewGuid(),
                CaseId = caseEntity.Id,
                FileName = Path.GetFileName(saved.LocalPath),
                FileExtension = ".pdf",
                ContentType = "application/pdf",
                SizeBytes = saved.SizeBytes,
                Category = category,
                StorageProvider = "local",
                LocalPath = saved.LocalPath,
                UploadedAt = DateTimeOffset.UtcNow,
                UploadedBy = "mail-pipeline",
            };
            db.CaseFiles.Add(pdfFile);

            // Apply status + publication updates
            caseEntity.Status = MailStatusRules.NextStatusFor(caseEntity.Status, classification);

            var liveFiles = await db.CaseFiles
                .AsNoTracking()
                .Where(f => f.CaseId == caseEntity.Id && f.DeletedAt == null)
                .ToListAsync(ct);
            var allFiles = liveFiles.Concat([pdfFile]).ToList();
            caseEntity.PublicationStatus = PublicationEligibility.Evaluate(caseEntity, allFiles);
            caseEntity.UpdatedAt = DateTimeOffset.UtcNow;

            // MailEvent
            db.MailEvents.Add(NewEvent(mail, classification, match,
                processingStatus: MailProcessingStatus.Processed,
                pdfFileId: pdfFile.Id,
                processedAt: DateTimeOffset.UtcNow));

            await db.SaveChangesAsync(ct);
            return Outcome.Processed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process mail {MessageId}", mail.MessageId);

            db.ChangeTracker.Clear();
            db.MailEvents.Add(NewEvent(mail, classification, match,
                processingStatus: MailProcessingStatus.FailedPdf,
                error: Truncate(ex.Message, 2048)));
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (Exception saveEx)
            {
                logger.LogError(saveEx, "Failed to even record the failure for mail {MessageId}", mail.MessageId);
            }
            return Outcome.Failed;
        }
    }

    private static MailEvent NewEvent(
        InboundMail mail,
        MailClassification classification,
        MatchResult match,
        MailProcessingStatus processingStatus,
        Guid? pdfFileId = null,
        DateTimeOffset? processedAt = null,
        string? error = null)
        => new()
        {
            Id = Guid.NewGuid(),
            ExternalMessageId = mail.MessageId,
            From = mail.From,
            Subject = mail.Subject,
            BodyPreview = Truncate(mail.TextBody, 2048),
            ReceivedAt = mail.ReceivedAt,
            Classification = classification,
            MatchConfidence = match.Confidence,
            CaseId = match.CaseId,
            PdfFileId = pdfFileId,
            ProcessedAt = processedAt,
            ProcessingStatus = processingStatus,
            ProcessingError = error,
        };

    private static string BuildPdfName(MailClassification classification, DateTimeOffset receivedAt)
        => $"{classification}-{receivedAt.LocalDateTime:yyyy-MM-dd-HHmm}.pdf";

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= max ? value : value[..max];
    }

    private enum Outcome { Processed, AlreadyProcessed, ManualReview, Failed }
}
