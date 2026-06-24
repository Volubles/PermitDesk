using Microsoft.EntityFrameworkCore;
using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Data;

public class PermitDeskDbContext(DbContextOptions<PermitDeskDbContext> options)
    : DbContext(options)
{
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CaseFile> CaseFiles => Set<CaseFile>();
    public DbSet<MailEvent> MailEvents => Set<MailEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PermitDeskDbContext).Assembly);
    }
}
