using Microsoft.EntityFrameworkCore;
using Voluble.PermitDesk.Web.Cases;
using Voluble.PermitDesk.Web.Domain;
using Voluble.PermitDesk.Web.Files;

namespace Voluble.PermitDesk.Web.Data;

public static class SeedData
{
    private record SeedCase(
        string Title,
        string Address,
        string Municipality,
        string Broker,
        string Photographer,
        string Email,
        string Phone,
        CaseStatus Status,
        CaseFileCategory[] FilesToAttach);

    private static readonly SeedCase[] Cases =
    [
        new("Storgatan 4", "Storgatan 4", "Borgholm",
            "Anna Lindqvist", "Erik Nyström", "anna@example.se", "070-111 22 33",
            CaseStatus.MaterialMissing, []),

        new("Köpmangatan 12", "Köpmangatan 12", "Borgholm",
            "Anna Lindqvist", "Erik Nyström", "anna@example.se", "070-111 22 33",
            CaseStatus.MaterialReady, [CaseFileCategory.Material, CaseFileCategory.Material]),

        new("Sandgatan 7B", "Sandgatan 7B", "Borgholm",
            "Petter Hellström", "Erik Nyström", "petter@example.se", "070-222 33 44",
            CaseStatus.ApplicationSubmitted,
            [CaseFileCategory.Material, CaseFileCategory.Material, CaseFileCategory.Application]),

        new("Trädgårdsgatan 3", "Trädgårdsgatan 3", "Mörbylånga",
            "Petter Hellström", "Sofia Berg", "petter@example.se", "070-222 33 44",
            CaseStatus.WaitingForDecision,
            [CaseFileCategory.Material, CaseFileCategory.Application, CaseFileCategory.SubmissionConfirmation]),

        new("Stationsgatan 18", "Stationsgatan 18", "Mörbylånga",
            "Karin Söderberg", "Sofia Berg", "karin@example.se", "070-333 44 55",
            CaseStatus.WaitingForDecision,
            [CaseFileCategory.Material, CaseFileCategory.Material, CaseFileCategory.Application]),

        new("Strandvägen 22", "Strandvägen 22", "Färjestaden",
            "Karin Söderberg", "Sofia Berg", "karin@example.se", "070-333 44 55",
            CaseStatus.Approved,
            [CaseFileCategory.Material, CaseFileCategory.Application, CaseFileCategory.SubmissionConfirmation, CaseFileCategory.Decision]),

        new("Strandvägen 41", "Strandvägen 41", "Färjestaden",
            "Anna Lindqvist", "Erik Nyström", "anna@example.se", "070-111 22 33",
            CaseStatus.Approved,
            [CaseFileCategory.Material, CaseFileCategory.Material, CaseFileCategory.Application, CaseFileCategory.Decision]),

        new("Skogsvägen 5", "Skogsvägen 5", "Löttorp",
            "Anna Lindqvist", "Sofia Berg", "anna@example.se", "070-111 22 33",
            CaseStatus.Approved,
            [CaseFileCategory.Material, CaseFileCategory.Application]),

        new("Hamnvägen 9", "Hamnvägen 9", "Byxelkrok",
            "Petter Hellström", "Erik Nyström", "petter@example.se", "070-222 33 44",
            CaseStatus.Rejected,
            [CaseFileCategory.Material, CaseFileCategory.Application, CaseFileCategory.Decision]),

        new("Norra Bygatan 14", "Norra Bygatan 14", "Borgholm",
            "Karin Söderberg", "Erik Nyström", "karin@example.se", "070-333 44 55",
            CaseStatus.MaterialMissing, []),

        new("Solgården 2", "Solgården 2", "Mörbylånga",
            "Anna Lindqvist", "Sofia Berg", "anna@example.se", "070-111 22 33",
            CaseStatus.MaterialReady,
            [CaseFileCategory.Material, CaseFileCategory.Material, CaseFileCategory.Material]),

        new("Kyrkvägen 8", "Kyrkvägen 8", "Borgholm",
            "Petter Hellström", "Sofia Berg", "petter@example.se", "070-222 33 44",
            CaseStatus.ApplicationSubmitted,
            [CaseFileCategory.Material, CaseFileCategory.Application]),

        new("Östra Långgatan 23", "Östra Långgatan 23", "Borgholm",
            "Karin Söderberg", "Erik Nyström", "karin@example.se", "070-333 44 55",
            CaseStatus.ManualReviewRequired,
            [CaseFileCategory.Material, CaseFileCategory.SubmissionConfirmation]),

        new("Bäckvägen 1", "Bäckvägen 1", "Färjestaden",
            "Anna Lindqvist", "Erik Nyström", "anna@example.se", "070-111 22 33",
            CaseStatus.WaitingForDecision,
            [CaseFileCategory.Material, CaseFileCategory.Application, CaseFileCategory.SubmissionConfirmation]),

        new("Sjövägen 33", "Sjövägen 33", "Löttorp",
            "Petter Hellström", "Sofia Berg", "petter@example.se", "070-222 33 44",
            CaseStatus.Approved,
            [CaseFileCategory.Material, CaseFileCategory.Material, CaseFileCategory.Application, CaseFileCategory.SubmissionConfirmation, CaseFileCategory.Decision]),
    ];

    public static async Task RunAsync(
        IDbContextFactory<PermitDeskDbContext> dbFactory,
        CaseNumberGenerator numberGenerator,
        ICaseStorage storage,
        CancellationToken ct = default)
    {
        Console.WriteLine("Seeding PermitDesk with realistic test data...");

        var inserted = 0;
        var skipped = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var seed in Cases)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            // Idempotency: skip if a case with this address already exists in the same year.
            var alreadyExists = await db.Cases
                .AnyAsync(c => c.Address == seed.Address && c.CreatedAt.Year == now.Year, ct);
            if (alreadyExists)
            {
                skipped++;
                continue;
            }

            var caseNumber = await numberGenerator.GenerateNextAsync(now, ct);
            var newCase = new Case
            {
                Id = Guid.NewGuid(),
                CaseNumber = caseNumber,
                Title = seed.Title,
                Address = seed.Address,
                Municipality = seed.Municipality,
                BrokerName = seed.Broker,
                PhotographerName = seed.Photographer,
                ContactEmail = seed.Email,
                ContactPhone = seed.Phone,
                Status = seed.Status,
                PublicationStatus = PublicationStatus.NotAllowed,
                CreatedAt = now.AddDays(-Random.Shared.Next(1, 60)),
                UpdatedAt = now.AddHours(-Random.Shared.Next(1, 72)),
            };

            newCase.LocalFolderPath = await storage.CreateCaseFolderAsync(newCase, ct);

            db.Cases.Add(newCase);

            // Attach placeholder files
            foreach (var category in seed.FilesToAttach)
            {
                var fileName = PlaceholderFileName(category);
                await using var stream = new MemoryStream(Placeholder(category));
                var saved = await storage.SaveFileAsync(newCase, category, fileName, stream, ct);

                db.CaseFiles.Add(new CaseFile
                {
                    Id = Guid.NewGuid(),
                    CaseId = newCase.Id,
                    FileName = Path.GetFileName(saved.LocalPath),
                    FileExtension = Path.GetExtension(saved.LocalPath),
                    ContentType = ContentTypeFor(Path.GetExtension(saved.LocalPath)),
                    SizeBytes = saved.SizeBytes,
                    Category = category,
                    StorageProvider = "local",
                    LocalPath = saved.LocalPath,
                    UploadedAt = newCase.CreatedAt.AddHours(Random.Shared.Next(1, 48)),
                });
            }

            // Recompute publication status after files are staged so the badge is correct.
            var allFiles = db.ChangeTracker.Entries<CaseFile>()
                .Where(e => e.State == EntityState.Added && ((CaseFile)e.Entity).CaseId == newCase.Id)
                .Select(e => (CaseFile)e.Entity)
                .ToList();
            newCase.PublicationStatus = PublicationEligibility.Evaluate(newCase, allFiles);

            await db.SaveChangesAsync(ct);
            inserted++;
        }

        Console.WriteLine($"Seed complete. Inserted {inserted}, skipped {skipped} (already existed).");
    }

    private static string PlaceholderFileName(CaseFileCategory category) => category switch
    {
        CaseFileCategory.Material => $"DJI_{Random.Shared.Next(1, 999):0000}.jpg",
        CaseFileCategory.Application => "ansokan.pdf",
        CaseFileCategory.SubmissionConfirmation => "inskickningsbekraftelse.pdf",
        CaseFileCategory.Decision => "beslut.pdf",
        CaseFileCategory.Other => "ovrigt.txt",
        _ => "fil.bin",
    };

    private static byte[] Placeholder(CaseFileCategory category) => category switch
    {
        CaseFileCategory.Material => new byte[1024],
        _ => System.Text.Encoding.UTF8.GetBytes($"Placeholder for {category} — seeded {DateTimeOffset.UtcNow:o}\n"),
    };

    private static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".txt" => "text/plain",
        _ => "application/octet-stream",
    };
}
