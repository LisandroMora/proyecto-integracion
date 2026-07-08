using Nomina.Domain.Enums;

namespace Nomina.Domain.Entities;

public class Transaccion
{
    public int Id { get; set; }
    public int EmpleadoId { get; set; }
    public TipoTransaccion TipoTransaccion { get; set; }
    public int ConceptoId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;

    public Empleado Empleado { get; set; } = null!;
}
