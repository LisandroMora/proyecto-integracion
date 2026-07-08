using System.ComponentModel.DataAnnotations;
using Nomina.Domain.Enums;

namespace Nomina.Application.DTOs;

public class TransaccionUpdateDto
{
    [Range(1, int.MaxValue)]
    public int EmpleadoId { get; set; }

    [Required]
    public TipoTransaccion TipoTransaccion { get; set; }

    [Range(1, int.MaxValue)]
    public int ConceptoId { get; set; }

    [Required]
    public DateTime Fecha { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El monto debe ser mayor o igual a cero.")]
    public decimal Monto { get; set; }

    [Required]
    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
}
