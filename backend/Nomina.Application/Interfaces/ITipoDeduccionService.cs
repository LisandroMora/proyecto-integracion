using Nomina.Application.Common;
using Nomina.Application.DTOs;

namespace Nomina.Application.Interfaces;

public interface ITipoDeduccionService
{
    Task<List<TipoDeduccionDto>> ListAsync(EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default);
    Task<TipoDeduccionDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TipoDeduccionDto> CreateAsync(TipoDeduccionCreateDto dto, CancellationToken ct = default);
    Task<TipoDeduccionDto?> UpdateAsync(int id, TipoDeduccionUpdateDto dto, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);
}
