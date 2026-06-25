using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Data.Configurations;

public class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CaseNumber).HasMaxLength(32);
        builder.HasIndex(c => c.CaseNumber).IsUnique();

        builder.Property(c => c.Title).HasMaxLength(256);
        builder.Property(c => c.Address).HasMaxLength(256);
        builder.Property(c => c.Municipality).HasMaxLength(128);
        builder.Property(c => c.BrokerName).HasMaxLength(128);
        builder.Property(c => c.PhotographerName).HasMaxLength(128);
        builder.Property(c => c.ContactEmail).HasMaxLength(256);
        builder.Property(c => c.ContactPhone).HasMaxLength(64);
        builder.Property(c => c.LantmaterietReferenceNumber).HasMaxLength(64);
        builder.Property(c => c.LocalFolderPath).HasMaxLength(1024);
        builder.Property(c => c.UploadLink).HasMaxLength(1024);

        builder.Property(c => c.MaterialTypes)
            .HasConversion<string>()
            .HasMaxLength(128);

        builder.Property(c => c.PurposeStatement).HasMaxLength(2048);

        builder.HasIndex(c => c.LantmaterietReferenceNumber);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(c => c.PublicationStatus)
            .HasConversion<string>()
            .HasMaxLength(16);
    }
}
