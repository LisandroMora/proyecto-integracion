using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nomina.Infrastructure.Persistence;

// Design-time factory: permite ejecutar `dotnet ef` sin depender del startup del API
// (necesario cuando el proceso Nomina.Api está corriendo y bloquea sus DLLs).
public class NominaDbContextFactory : IDesignTimeDbContextFactory<NominaDbContext>
{
    private const string LocalDbConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=NominaDb;Trusted_Connection=True;" +
        "MultipleActiveResultSets=true;TrustServerCertificate=True";

    public NominaDbContext CreateDbContext(string[] args)
    {
        // Por defecto apunta a LocalDB. Definir NOMINA_CONNECTION permite aplicar las
        // migraciones contra otra base —la de Azure, por ejemplo— sin tocar el código:
        //   $env:NOMINA_CONNECTION = "Server=tcp:...;"
        //   dotnet ef database update -p Nomina.Infrastructure -s Nomina.Api
        var connectionString = Environment.GetEnvironmentVariable("NOMINA_CONNECTION")
            ?? LocalDbConnectionString;

        var options = new DbContextOptionsBuilder<NominaDbContext>()
            .UseSqlServer(connectionString, sql => sql.CommandTimeout(180))
            .Options;

        return new NominaDbContext(options);
    }
}
