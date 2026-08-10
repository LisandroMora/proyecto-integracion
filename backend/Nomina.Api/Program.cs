using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nomina.Api.Middleware;
using Nomina.Application;
using Nomina.Infrastructure;
using Nomina.Infrastructure.Persistence;
using Nomina.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// App Service termina el TLS en su proxy y nos reenvía la petición por HTTP plano.
// Sin leer X-Forwarded-Proto la app cree que el cliente vino por HTTP y
// UseHttpsRedirection lo redirige otra vez a HTTPS: bucle infinito.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // La IP del proxy de la plataforma no se conoce de antemano y puede cambiar.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key no configurada.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// Lista, no origen único: en la nube conviven el backoffice desplegado y los
// entornos de staging que Static Web Apps crea por Pull Request.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Backoffice", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.UseForwardedHeaders();

// En local esto crea la base y el admin en el primer arranque; en el servidor de
// prueba aplica las migraciones pendientes en cada despliegue. Se puede apagar con
// Deploy:MigrateOnStartup=false si alguna vez se prefiere migrar desde el pipeline.
if (builder.Configuration.GetValue("Deploy:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NominaDbContext>();
    await db.Database.MigrateAsync();
    await DataSeeder.SeedAsync(db, builder.Configuration["Seed:AdminPassword"]);
}

// Se publica también fuera de Development: en el servidor de prueba el contrato
// REST es lo que revisan el equipo de Contabilidad y la evaluación de la materia.
app.MapOpenApi();

app.UseExceptionHandler();

// Sin UseHttpsRedirection a propósito: detrás de App Service el proceso solo
// escucha HTTP, así que el middleware no sabe a qué puerto redirigir y se
// desactiva solo ("Failed to determine the https port for redirect"). Quien
// obliga a HTTPS es la plataforma, con la opción "HTTPS Only" del Web App, que
// además redirige en el borde sin gastar cuota de CPU de la app.

app.UseCors("Backoffice");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Deliberadamente no toca la base de datos: sirve de sonda para la plataforma y
// para despertar la app antes de una demo, y consultar la BD la sacaría de la
// pausa automática, que es justo lo que mantiene el consumo dentro del plan gratuito.
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();
