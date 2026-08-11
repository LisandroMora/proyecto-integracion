using Microsoft.EntityFrameworkCore;
using Nomina.Application.Common;
using Nomina.Application.Interfaces;
using Nomina.Domain.Entities;
using Nomina.Domain.Enums;

namespace Nomina.Infrastructure.Persistence;

public class AsientoContableRepository : IAsientoContableRepository
{
    private readonly NominaDbContext _db;
    public AsientoContableRepository(NominaDbContext db) => _db = db;

    public Task<List<Transaccion>> GetTransaccionesSinContabilizarAsync(
        int anio, int mes, CancellationToken ct = default)
    {
        var inicio = new DateTime(anio, mes, 1);
        var fin = inicio.AddMonths(1);

        return _db.Transacciones
            .Where(t => t.Estado == EstadoRegistro.Activo
                        && t.AsientoContableId == null
                        && t.Fecha >= inicio
                        && t.Fecha < fin)
            .ToListAsync(ct);
    }

    public Task<List<Transaccion>> GetTransaccionesByAsientoAsync(int asientoId, CancellationToken ct = default) =>
        _db.Transacciones
            .Where(t => t.AsientoContableId == asientoId)
            .ToListAsync(ct);

    public Task<List<AsientoContable>> ListByPeriodoAsync(int anio, int mes, CancellationToken ct = default) =>
        _db.AsientosContables
            .Include(a => a.Detalles)
            .Where(a => a.Anio == anio && a.Mes == mes)
            .ToListAsync(ct);

    public Task<List<AsientoContable>> ListAsync(
        int? anio, int? mes, EstadoFilter filter = EstadoFilter.Activos, CancellationToken ct = default)
    {
        IQueryable<AsientoContable> q = _db.AsientosContables.Include(a => a.Detalles);

        // Un asiento reabierto queda inactivo: sigue en la base como evidencia del
        // envío perdido, pero por defecto no se lista.
        q = filter switch
        {
            EstadoFilter.Activos => q.Where(a => a.Estado == EstadoRegistro.Activo),
            EstadoFilter.Inactivos => q.Where(a => a.Estado == EstadoRegistro.Inactivo),
            _ => q
        };

        if (anio is int a1) q = q.Where(a => a.Anio == a1);
        if (mes is int m1) q = q.Where(a => a.Mes == m1);

        return q
            .OrderByDescending(a => a.Anio)
            .ThenByDescending(a => a.Mes)
            .ThenBy(a => a.TipoTransaccion)
            .ThenBy(a => a.ConceptoNombre)
            .ToListAsync(ct);
    }

    public Task<AsientoContable?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.AsientosContables
            .Include(a => a.Detalles)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task AddAsync(AsientoContable entity, CancellationToken ct = default) =>
        await _db.AsientosContables.AddAsync(entity, ct);

    public async Task<Dictionary<int, string>> GetTiposIngresoNamesAsync(CancellationToken ct = default)
    {
        var items = await _db.TiposIngreso.Select(x => new { x.Id, x.Nombre }).ToListAsync(ct);
        return items.ToDictionary(x => x.Id, x => x.Nombre);
    }

    public async Task<Dictionary<int, string>> GetTiposDeduccionNamesAsync(CancellationToken ct = default)
    {
        var items = await _db.TiposDeduccion.Select(x => new { x.Id, x.Nombre }).ToListAsync(ct);
        return items.ToDictionary(x => x.Id, x => x.Nombre);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
