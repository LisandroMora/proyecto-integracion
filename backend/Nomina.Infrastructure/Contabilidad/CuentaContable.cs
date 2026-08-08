namespace Nomina.Infrastructure.Contabilidad;

/// <summary>Cuenta tal como la devuelve GET /api/cuentas del Sistema de Contabilidad.</summary>
internal record CuentaContable(
    int Id,
    string? Codigo,
    string? Nombre,
    bool PermiteTransacciones,
    string? Estado);
