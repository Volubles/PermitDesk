using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Data.Configurations;

public class CaseFileConfiguration : IEntityTypeConfiguration<CaseFile>
{
    public void Configure(EntityTypeBuilder<CaseFile> builder)
    {
        builder.HasKey(f => f.Id);

        builder.HasOne(f => f.Case)
            .WithMany(c => c.Files)
            .HasForeignKey(f => f.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => f.CaseId);

        builder.Property(f => f.FileName).HasMaxLength(512);
        builder.Property(f => f.FileExtension).HasMaxLength(32);
        builder.Property(f => f.ContentType).HasMaxLength(128);
        builder.Property(f => f.StorageProvider).HasMaxLength(32);
        builder.Property(f => f.StorageItemId).HasMaxLength(256);
        builder.Property(f => f.StorageWebUrl).HasMaxLength(2048);
        builder.Property(f => f.LocalPath).HasMaxLength(1024);
        builder.Property(f => f.UploadedBy).HasMaxLength(256);

        builder.Property(f => f.Category)
            .HasConversion<string>()
            .HasMaxLength(32);
    }
}
