using Nomina.Application.Common;
using Nomina.Application.DTOs;

namespace Nomina.Application.Interfaces;

public interface ITransaccionService
{
    Task<List<TransaccionDto>> ListAsync(TransaccionQuery query, CancellationToken ct = default);
    Task<TransaccionDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TransaccionDto> CreateAsync(TransaccionCreateDto dto, CancellationToken ct = default);
    Task<TransaccionDto?> UpdateAsync(int id, TransaccionUpdateDto dto, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);
}
