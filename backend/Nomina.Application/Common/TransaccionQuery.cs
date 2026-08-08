using Nomina.Domain.Enums;

namespace Nomina.Application.Common;

/// <summary>
/// Criterios de búsqueda para la consulta de transacciones. Todos los criterios
/// son opcionales y se combinan con AND; sin criterios equivale a listar todo.
/// </summary>
public class TransaccionQuery
{
    public EstadoFilter Estado { get; set; } = EstadoFilter.Activos;
    public int? EmpleadoId { get; set; }
    public TipoTransaccion? TipoTransaccion { get; set; }
    public int? ConceptoId { get; set; }

    /// <summary>Inicio del rango de fechas, inclusivo.</summary>
    public DateTime? FechaDesde { get; set; }

    /// <summary>Fin del rango de fechas, inclusivo (cubre el día completo).</summary>
    public DateTime? FechaHasta { get; set; }
}
