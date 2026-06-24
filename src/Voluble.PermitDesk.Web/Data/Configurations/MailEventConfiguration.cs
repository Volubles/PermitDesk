using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Data.Configurations;

public class MailEventConfiguration : IEntityTypeConfiguration<MailEvent>
{
    public void Configure(EntityTypeBuilder<MailEvent> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.ExternalMessageId).HasMaxLength(998);
        builder.HasIndex(m => m.ExternalMessageId).IsUnique();

        builder.Property(m => m.From).HasMaxLength(512);
        builder.Property(m => m.Subject).HasMaxLength(998);
        builder.Property(m => m.BodyPreview).HasMaxLength(2048);
        builder.Property(m => m.ProcessingError).HasMaxLength(2048);

        builder.Property(m => m.Classification)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(m => m.MatchConfidence)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(m => m.ProcessingStatus)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(m => m.Case)
            .WithMany()
            .HasForeignKey(m => m.CaseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.PdfFile)
            .WithMany()
            .HasForeignKey(m => m.PdfFileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => m.CaseId);
        builder.HasIndex(m => m.ProcessingStatus);
        builder.HasIndex(m => m.ReceivedAt);
    }
}
