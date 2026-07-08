using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nomina.Infrastructure.Persistence;

// Design-time factory: permite ejecutar `dotnet ef` sin depender del startup del API
// (necesario cuando el proceso Nomina.Api está corriendo y bloquea sus DLLs).
public class NominaDbContextFactory : IDesignTimeDbContextFactory<NominaDbContext>
{
    public NominaDbContext CreateDbContext(string[] args)
    {
        const string connectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=NominaDb;Trusted_Connection=True;" +
            "MultipleActiveResultSets=true;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<NominaDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new NominaDbContext(options);
    }
}
