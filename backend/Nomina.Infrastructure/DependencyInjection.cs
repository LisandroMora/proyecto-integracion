using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nomina.Application.Interfaces;
using Nomina.Infrastructure.Auth;
using Nomina.Infrastructure.Contabilidad;
using Nomina.Infrastructure.Persistence;

namespace Nomina.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection no configurada.");

        services.AddDbContext<NominaDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(NominaDbContext).Assembly.FullName)));

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<ContabilidadSettings>(configuration.GetSection("Contabilidad"));

        var contabilidad = configuration.GetSection("Contabilidad").Get<ContabilidadSettings>()
            ?? new ContabilidadSettings();

        if (string.IsNullOrWhiteSpace(contabilidad.BaseUrl))
            throw new InvalidOperationException("Contabilidad:BaseUrl no configurada.");

        services.AddSingleton<CuentasContablesCache>();
        services.AddHttpClient<IContabilidadClient, ContabilidadHttpClient>(http =>
        {
            http.BaseAddress = new Uri(contabilidad.BaseUrl);
            // Su servidor gratuito puede tardar más que el timeout por defecto (100 s).
            http.Timeout = TimeSpan.FromSeconds(contabilidad.TimeoutSegundos);
        });

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ITipoIngresoRepository, TipoIngresoRepository>();
        services.AddScoped<ITipoDeduccionRepository, TipoDeduccionRepository>();
        services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
        services.AddScoped<INominaRepository, NominaRepository>();
        services.AddScoped<ITransaccionRepository, TransaccionRepository>();
        services.AddScoped<IAsientoContableRepository, AsientoContableRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
