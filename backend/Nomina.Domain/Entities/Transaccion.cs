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

    /// <summary>
    /// Asiento en el que esta transacción fue contabilizada. Null mientras esté
    /// pendiente de cierre. Una vez asignado, la transacción no se puede modificar
    /// ni anular: la corrección se hace con una transacción de ajuste.
    /// </summary>
    public int? AsientoContableId { get; set; }

    public Empleado Empleado { get; set; } = null!;
    public AsientoContable? AsientoContable { get; set; }
}
