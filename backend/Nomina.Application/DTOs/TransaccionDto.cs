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
    EstadoRegistro Estado);
