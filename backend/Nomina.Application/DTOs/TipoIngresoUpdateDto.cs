using System.ComponentModel.DataAnnotations;
using Nomina.Domain.Enums;

namespace Nomina.Application.DTOs;

public class TipoIngresoUpdateDto
{
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public bool DependeDeSalario { get; set; }

    [Range(0.01, 100.00)]
    public decimal? Porcentaje { get; set; }

    [Required]
    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
}
