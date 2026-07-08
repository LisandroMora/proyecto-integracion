using Nomina.Application.Common;
using Nomina.Application.DTOs;

namespace Nomina.Application.Interfaces;

public interface IEmpleadoService
{
    Task<List<EmpleadoDto>> ListAsync(EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default);
    Task<EmpleadoDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<EmpleadoDto> CreateAsync(EmpleadoCreateDto dto, CancellationToken ct = default);
    Task<EmpleadoDto?> UpdateAsync(int id, EmpleadoUpdateDto dto, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);
}
