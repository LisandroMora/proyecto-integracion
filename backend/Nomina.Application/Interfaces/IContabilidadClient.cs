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
/// Puerto hacia el Sistema de Contabilidad. La implementación concreta vive en
/// Infrastructure para que la lógica de nómina no dependa del transporte.
/// </summary>
public interface IContabilidadClient
{
    /// <summary>
    /// Determina qué cuentas usar según la naturaleza del concepto, resolviendo el
    /// código configurado contra el catálogo de Contabilidad.
    /// </summary>
    Task<CuentasAsiento> ResolverCuentasAsync(TipoTransaccion tipo, CancellationToken ct = default);

    Task<AsientoRegistradoResponse> RegistrarAsientoAsync(
        int cuentaDebitoId,
        int cuentaCreditoId,
        string descripcion,
        decimal monto,
        CancellationToken ct = default);
}
