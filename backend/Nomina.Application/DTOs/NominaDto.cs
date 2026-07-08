using Nomina.Domain.Enums;

namespace Nomina.Application.DTOs;

public record NominaDto(int Id, string Nombre, EstadoRegistro Estado);
