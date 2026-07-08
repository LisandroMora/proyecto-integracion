using Nomina.Application.Common;
using Nomina.Application.DTOs;
using Nomina.Application.Exceptions;
using Nomina.Application.Interfaces;
using Nomina.Domain.Entities;
using Nomina.Domain.Enums;

namespace Nomina.Application.Services;

public class TransaccionService : ITransaccionService
{
    private readonly ITransaccionRepository _repo;

    public TransaccionService(ITransaccionRepository repo) => _repo = repo;

    public async Task<List<TransaccionDto>> ListAsync(EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default)
    {
        var items = await _repo.ListAsync(filter, ct);
        var ingresos = await _repo.GetTiposIngresoNamesAsync(ct);
        var deducciones = await _repo.GetTiposDeduccionNamesAsync(ct);
        return items.Select(t => Map(t, ingresos, deducciones)).ToList();
    }

    public async Task<TransaccionDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity is null) return null;

        var conceptoNombre = await ResolveConceptoNombreAsync(entity.TipoTransaccion, entity.ConceptoId, ct);
        return Map(entity, conceptoNombre);
    }

    public async Task<TransaccionDto> CreateAsync(TransaccionCreateDto dto, CancellationToken ct = default)
    {
        await ValidateAsync(dto.EmpleadoId, dto.TipoTransaccion, dto.ConceptoId, ct);

        var entity = new Transaccion
        {
            EmpleadoId = dto.EmpleadoId,
            TipoTransaccion = dto.TipoTransaccion,
            ConceptoId = dto.ConceptoId,
            Fecha = dto.Fecha,
            Monto = dto.Monto,
            Estado = EstadoRegistro.Activo
        };

        await _repo.AddAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);

        var reloaded = await _repo.GetByIdAsync(entity.Id, ct);
        var conceptoNombre = await ResolveConceptoNombreAsync(reloaded!.TipoTransaccion, reloaded.ConceptoId, ct);
        return Map(reloaded, conceptoNombre);
    }

    public async Task<TransaccionDto?> UpdateAsync(int id, TransaccionUpdateDto dto, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity is null) return null;

        await ValidateAsync(dto.EmpleadoId, dto.TipoTransaccion, dto.ConceptoId, ct);

        entity.EmpleadoId = dto.EmpleadoId;
        entity.TipoTransaccion = dto.TipoTransaccion;
        entity.ConceptoId = dto.ConceptoId;
        entity.Fecha = dto.Fecha;
        entity.Monto = dto.Monto;
        entity.Estado = dto.Estado;

        await _repo.SaveChangesAsync(ct);

        var reloaded = await _repo.GetByIdAsync(id, ct);
        var conceptoNombre = await ResolveConceptoNombreAsync(reloaded!.TipoTransaccion, reloaded.ConceptoId, ct);
        return Map(reloaded, conceptoNombre);
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(id, ct);
        if (entity is null) return false;

        entity.Estado = EstadoRegistro.Inactivo;
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    private async Task ValidateAsync(int empleadoId, TipoTransaccion tipo, int conceptoId, CancellationToken ct)
    {
        if (!await _repo.EmpleadoExistsAsync(empleadoId, ct))
            throw new DomainValidationException("El empleado indicado no existe.");

        var lookup = tipo == TipoTransaccion.Ingreso
            ? await _repo.GetTiposIngresoNamesAsync(ct)
            : await _repo.GetTiposDeduccionNamesAsync(ct);

        if (!lookup.ContainsKey(conceptoId))
        {
            var origen = tipo == TipoTransaccion.Ingreso ? "Tipos de Ingreso" : "Tipos de Deducción";
            throw new DomainValidationException(
                $"El concepto indicado no existe en el catálogo de {origen}.");
        }
    }

    private async Task<string> ResolveConceptoNombreAsync(TipoTransaccion tipo, int conceptoId, CancellationToken ct)
    {
        var lookup = tipo == TipoTransaccion.Ingreso
            ? await _repo.GetTiposIngresoNamesAsync(ct)
            : await _repo.GetTiposDeduccionNamesAsync(ct);
        return lookup.TryGetValue(conceptoId, out var name) ? name : $"(id {conceptoId})";
    }

    private static TransaccionDto Map(
        Transaccion t,
        IReadOnlyDictionary<int, string> ingresos,
        IReadOnlyDictionary<int, string> deducciones)
    {
        var lookup = t.TipoTransaccion == TipoTransaccion.Ingreso ? ingresos : deducciones;
        var nombre = lookup.TryGetValue(t.ConceptoId, out var n) ? n : $"(id {t.ConceptoId})";
        return Map(t, nombre);
    }

    private static TransaccionDto Map(Transaccion t, string conceptoNombre) => new(
        t.Id,
        t.EmpleadoId,
        t.Empleado?.Cedula ?? string.Empty,
        t.Empleado?.Nombre ?? string.Empty,
        t.TipoTransaccion,
        t.ConceptoId,
        conceptoNombre,
        t.Fecha,
        t.Monto,
        t.Estado);
}
