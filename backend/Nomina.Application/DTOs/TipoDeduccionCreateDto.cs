using System.ComponentModel.DataAnnotations;

namespace Nomina.Application.DTOs;

public class TipoDeduccionCreateDto
{
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    public bool DependeDeSalario { get; set; }

    [Range(0.01, 100.00)]
    public decimal? Porcentaje { get; set; }
}
