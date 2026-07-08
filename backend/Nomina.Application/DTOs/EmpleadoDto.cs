using Nomina.Domain.Enums;

namespace Nomina.Application.DTOs;

public record EmpleadoDto(
    int Id,
    string Cedula,
    string Nombre,
    string? Departamento,
    string? Puesto,
    decimal SalarioMensual,
    int NominaId,
    string NominaNombre,
    EstadoRegistro Estado);
