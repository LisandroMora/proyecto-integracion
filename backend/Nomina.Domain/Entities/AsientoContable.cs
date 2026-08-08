using Nomina.Domain.Enums;

namespace Nomina.Domain.Entities;

/// <summary>
/// Asiento contable de un concepto (tipo de ingreso o deducción) dentro de un
/// período de nómina. Agrupa todas las transacciones activas de ese concepto en
/// el período y es la unidad que se envía al Sistema de Contabilidad.
/// </summary>
public class AsientoContable
{
    public int Id { get; set; }

    public int Anio { get; set; }
    public int Mes { get; set; }

    public TipoTransaccion TipoTransaccion { get; set; }
    public int ConceptoId { get; set; }

    /// <summary>Nombre del concepto al momento de generar el asiento.</summary>
    public string ConceptoNombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateTime FechaAsiento { get; set; }

    /// <summary>Cantidad de transacciones que componen el monto.</summary>
    public int CantidadTransacciones { get; set; }

    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;

    public EstadoEnvioAsiento EstadoEnvio { get; set; } = EstadoEnvioAsiento.Pendiente;

    /// <summary>Identificador que devuelve Contabilidad; null hasta que el envío es aceptado.</summary>
    public int? NumeroAsiento { get; set; }

    public DateTime? FechaEnvio { get; set; }
    public string? MensajeError { get; set; }

    public ICollection<AsientoContableDetalle> Detalles { get; set; } = new List<AsientoContableDetalle>();

    /// <summary>Transacciones que componen el monto. Permite llegar al empleado desde el asiento.</summary>
    public ICollection<Transaccion> Transacciones { get; set; } = new List<Transaccion>();
}
