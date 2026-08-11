using Nomina.Domain.Enums;

namespace Nomina.Application.Interfaces;

/// <summary>Cuentas resueltas para armar un asiento, ya con el id que espera Contabilidad.</summary>
public record CuentasAsiento(
    int DebitoId,
    string DebitoCodigo,
    string DebitoNombre,
    int CreditoId,
    string CreditoCodigo,
    string CreditoNombre);

/// <summary>Respuesta de Contabilidad al registrar un asiento.</summary>
public record AsientoRegistradoResponse(
    int NumeroAsiento,
    DateTime Fecha,
    string Estado,
    string Mensaje);

/// <summary>
/// Asiento tal como Contabilidad lo tiene hoy, ya consolidado a partir de las
/// líneas que devuelve su API.
/// </summary>
public record EntradaContabilidad(
    int? NumeroAsiento,
    string Descripcion,
    decimal Monto,
    DateTime? Fecha,
    string Estado)
{
    public bool EstaActiva => string.Equals(Estado, "ACTIVO", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Puerto hacia el Sistema de Contabilidad. La implementación concreta vive en
/// Infrastructure para que la lógica de nómina no dependa del transporte.
/// </summary>
public interface IContabilidadClient
{
    /// <summary>
    /// Par de cuentas de todo asiento de nómina, resolviendo los códigos
    /// configurados contra el catálogo de Contabilidad.
    /// </summary>
    Task<CuentasAsiento> ResolverCuentasAsync(CancellationToken ct = default);

    Task<AsientoRegistradoResponse> RegistrarAsientoAsync(
        int cuentaDebitoId,
        int cuentaCreditoId,
        string descripcion,
        decimal monto,
        CancellationToken ct = default);

    /// <summary>
    /// Asientos que Contabilidad tiene registrados bajo nuestro auxiliar. Su API
    /// solo filtra por auxiliar —ni por período ni por número—, así que se trae
    /// todo lo nuestro y el cruce se resuelve de este lado.
    /// </summary>
    Task<List<EntradaContabilidad>> ConsultarEntradasAsync(CancellationToken ct = default);
}
