using System.ComponentModel.DataAnnotations;
using Nomina.Domain.Enums;

namespace Nomina.Application.DTOs;

public class EmpleadoUpdateDto
{
    [Required, MaxLength(20)]
    public string Cedula { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Departamento { get; set; }

    [MaxLength(100)]
    public string? Puesto { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El salario debe ser mayor o igual a cero.")]
    public decimal SalarioMensual { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una nómina válida.")]
    public int NominaId { get; set; }

    [Required]
    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
}
