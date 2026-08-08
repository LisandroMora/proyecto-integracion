using Nomina.Domain.Entities;

namespace Nomina.Application.Interfaces;

public interface IAsientoContableRepository
{
    /// <summary>
    /// Transacciones activas del período que todavía no forman parte de ningún
    /// asiento. Se devuelven las entidades, no un resumen, para poder marcarlas
    /// como contabilizadas con el mismo conjunto que se sumó.
    /// </summary>
    Task<List<Transaccion>> GetTransaccionesSinContabilizarAsync(
        int anio, int mes, CancellationToken ct = default);

    Task<List<AsientoContable>> ListByPeriodoAsync(int anio, int mes, CancellationToken ct = default);
    Task<List<AsientoContable>> ListAsync(int? anio, int? mes, CancellationToken ct = default);
    Task<AsientoContable?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(AsientoContable entity, CancellationToken ct = default);

    Task<Dictionary<int, string>> GetTiposIngresoNamesAsync(CancellationToken ct = default);
    Task<Dictionary<int, string>> GetTiposDeduccionNamesAsync(CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
