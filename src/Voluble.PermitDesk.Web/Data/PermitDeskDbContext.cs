using Microsoft.EntityFrameworkCore;

namespace Voluble.PermitDesk.Web.Data;

public class PermitDeskDbContext(DbContextOptions<PermitDeskDbContext> options)
    : DbContext(options)
{
}
