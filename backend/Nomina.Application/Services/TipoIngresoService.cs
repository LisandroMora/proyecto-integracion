using Nomina.Application.Common;
using Nomina.Application.DTOs;
using Nomina.Application.Exceptions;
using Nomina.Application.Interfaces;
using Nomina.Domain.Entities;
using Nomina.Domain.Enums;

namespace Nomina.Application.Services;

public class TipoIngresoService : ITipoIngresoService
{
    private readonly ITipoIngresoRepository _repo;

    public TipoIngresoService(ITipoIngresoRepository repo) => _repo = repo;

    public async Task<List<TipoIngresoDto>> ListAsync(EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default)
    {
        var items = await _repo.ListAsync(filter, ct);
        return items.Select(Map).ToList();
    }

    public async Task<TipoIngresoDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, ct);
        return item is null ? null : Map(item);
    }

    public async Task<TipoIngresoDto> CreateAsync(TipoIngresoCreateDto dto, CancellationToken ct = default)
    {
        var porcentaje = NormalizePorcentaje(dto.DependeDeSalario, dto.Porcentaje);

        var entity = new TipoIngreso
        {
            Nombre = dto.Nombre.Trim(),
            DependeDeSalario = dto.DependeDeSalario,
            Porcentaje = porcentaje,
            Estado = EstadoRegistro.Activo
        };
        await _repo.AddAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<TipoIngresoDto?> UpdateAsync(int id, TipoIngresoUpdateDto dto, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity is null) return null;

        var porcentaje = NormalizePorcentaje(dto.DependeDeSalario, dto.Porcentaje);

        entity.Nombre = dto.Nombre.Trim();
        entity.DependeDeSalario = dto.DependeDeSalario;
        entity.Porcentaje = porcentaje;
        entity.Estado = dto.Estado;

        await _repo.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity is null) return false;

        entity.Estado = EstadoRegistro.Inactivo;
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    // Si depende del salario: porcentaje obligatorio en (0, 100]. Si no depende: se descarta.
    private static decimal? NormalizePorcentaje(bool dependeDeSalario, decimal? porcentaje)
    {
        if (!dependeDeSalario) return null;
        if (porcentaje is null || porcentaje <= 0 || porcentaje > 100)
            throw new DomainValidationException(
                "El porcentaje es obligatorio y debe estar entre 0.01 y 100 cuando depende del salario.");
        return porcentaje;
    }

    private static TipoIngresoDto Map(TipoIngreso e) =>
        new(e.Id, e.Nombre, e.DependeDeSalario, e.Porcentaje, e.Estado);
}
