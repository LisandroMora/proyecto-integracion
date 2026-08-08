using Nomina.Domain.Enums;

namespace Nomina.Application.DTOs;

public record TransaccionDto(
    int Id,
    int EmpleadoId,
    string EmpleadoCedula,
    string EmpleadoNombre,
    TipoTransaccion TipoTransaccion,
    int ConceptoId,
    string ConceptoNombre,
    DateTime Fecha,
    decimal Monto,
    EstadoRegistro Estado,
    /// <summary>Asiento local que la contabilizó; null si aún está pendiente de cierre.</summary>
    int? AsientoContableId,
    /// <summary>Número que asignó Contabilidad al asiento correspondiente.</summary>
    int? NumeroAsiento);
