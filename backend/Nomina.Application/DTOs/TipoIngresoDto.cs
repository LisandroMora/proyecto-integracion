using Nomina.Domain.Enums;

namespace Nomina.Application.DTOs;

public record TipoIngresoDto(
    int Id,
    string Nombre,
    bool DependeDeSalario,
    decimal? Porcentaje,
    EstadoRegistro Estado);
