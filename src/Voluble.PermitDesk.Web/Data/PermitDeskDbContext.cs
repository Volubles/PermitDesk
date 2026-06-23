using Microsoft.EntityFrameworkCore;
using Voluble.PermitDesk.Web.Domain;

namespace Voluble.PermitDesk.Web.Data;

public class PermitDeskDbContext(DbContextOptions<PermitDeskDbContext> options)
    : DbContext(options)
{
    public DbSet<Case> Cases => Set<Case>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PermitDeskDbContext).Assembly);
    }
}
