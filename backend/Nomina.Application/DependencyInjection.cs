using Microsoft.Extensions.DependencyInjection;
using Nomina.Application.Interfaces;
using Nomina.Application.Services;

namespace Nomina.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITipoIngresoService, TipoIngresoService>();
        services.AddScoped<ITipoDeduccionService, TipoDeduccionService>();
        services.AddScoped<IEmpleadoService, EmpleadoService>();
        services.AddScoped<INominaService, NominaService>();
        services.AddScoped<ITransaccionService, TransaccionService>();
        return services;
    }
}
