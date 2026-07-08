using Nomina.Application.Common;
using Nomina.Application.DTOs;
using Nomina.Application.Exceptions;
using Nomina.Application.Interfaces;
using Nomina.Application.Validation;
using Nomina.Domain.Entities;
using Nomina.Domain.Enums;

namespace Nomina.Application.Services;

public class EmpleadoService : IEmpleadoService
{
    private readonly IEmpleadoRepository _repo;

    public EmpleadoService(IEmpleadoRepository repo) => _repo = repo;

    public async Task<List<EmpleadoDto>> ListAsync(EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default)
    {
        var items = await _repo.ListAsync(filter, ct);
        return items.Select(Map).ToList();
    }

    public async Task<EmpleadoDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, ct);
        return item is null ? null : Map(item);
    }

    public async Task<EmpleadoDto> CreateAsync(EmpleadoCreateDto dto, CancellationToken ct = default)
    {
        var cedula = CedulaValidator.Normalize(dto.Cedula);

        if (!CedulaValidator.EsValida(cedula))
            throw new DomainValidationException("La cédula ingresada no es válida.");

        if (!await _repo.NominaExistsAsync(dto.NominaId, ct))
            throw new DomainValidationException("La nómina indicada no existe.");

        if (await _repo.ExistsCedulaAsync(cedula, excludeId: null, ct))
            throw new DomainValidationException($"Ya existe un empleado con la cédula {cedula}.", 409);

        var entity = new Empleado
        {
            Cedula = cedula,
            Nombre = dto.Nombre.Trim(),
            Departamento = string.IsNullOrWhiteSpace(dto.Departamento) ? null : dto.Departamento.Trim(),
            Puesto = string.IsNullOrWhiteSpace(dto.Puesto) ? null : dto.Puesto.Trim(),
            SalarioMensual = dto.SalarioMensual,
            NominaId = dto.NominaId,
            Estado = EstadoRegistro.Activo
        };

        await _repo.AddAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);

        // Reload to hydrate Nomina navigation for the DTO
        var created = await _repo.GetByIdAsync(entity.Id, ct);
        return Map(created!);
    }

    public async Task<EmpleadoDto?> UpdateAsync(int id, EmpleadoUpdateDto dto, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity is null) return null;

        var cedula = CedulaValidator.Normalize(dto.Cedula);

        if (!CedulaValidator.EsValida(cedula))
            throw new DomainValidationException("La cédula ingresada no es válida.");

        if (!await _repo.NominaExistsAsync(dto.NominaId, ct))
            throw new DomainValidationException("La nómina indicada no existe.");

        if (await _repo.ExistsCedulaAsync(cedula, excludeId: id, ct))
            throw new DomainValidationException($"Ya existe otro empleado con la cédula {cedula}.", 409);

        entity.Cedula = cedula;
        entity.Nombre = dto.Nombre.Trim();
        entity.Departamento = string.IsNullOrWhiteSpace(dto.Departamento) ? null : dto.Departamento.Trim();
        entity.Puesto = string.IsNullOrWhiteSpace(dto.Puesto) ? null : dto.Puesto.Trim();
        entity.SalarioMensual = dto.SalarioMensual;
        entity.NominaId = dto.NominaId;
        entity.Estado = dto.Estado;

        await _repo.SaveChangesAsync(ct);

        var updated = await _repo.GetByIdAsync(id, ct);
        return Map(updated!);
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity is null) return false;

        entity.Estado = EstadoRegistro.Inactivo;
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    private static EmpleadoDto Map(Empleado e) => new(
        e.Id,
        e.Cedula,
        e.Nombre,
        e.Departamento,
        e.Puesto,
        e.SalarioMensual,
        e.NominaId,
        e.Nomina?.Nombre ?? string.Empty,
        e.Estado);
}
