using Nomina.Application.Common;
using Nomina.Application.DTOs;

namespace Nomina.Application.Interfaces;

public interface ITipoIngresoService
{
    Task<List<TipoIngresoDto>> ListAsync(EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default);
    Task<TipoIngresoDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TipoIngresoDto> CreateAsync(TipoIngresoCreateDto dto, CancellationToken ct = default);
    Task<TipoIngresoDto?> UpdateAsync(int id, TipoIngresoUpdateDto dto, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);
}
