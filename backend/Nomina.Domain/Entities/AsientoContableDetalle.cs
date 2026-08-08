using Nomina.Domain.Enums;

namespace Nomina.Domain.Entities;

/// <summary>
/// Línea de un asiento: una cuenta con su naturaleza (DB/CR) y su monto.
/// Cada asiento genera exactamente dos líneas y ambas deben cuadrar.
/// </summary>
public class AsientoContableDetalle
{
    public int Id { get; set; }
    public int AsientoContableId { get; set; }

    /// <summary>Id de la cuenta en el Sistema de Contabilidad.</summary>
    public int Cuenta { get; set; }

    /// <summary>Código contable de la cuenta (501, 202, 201). Estable ante recargas de su base.</summary>
    public string CuentaCodigo { get; set; } = string.Empty;

    public string CuentaNombre { get; set; } = string.Empty;

    public TipoMovimiento TipoMovimiento { get; set; }
    public decimal Monto { get; set; }

    public AsientoContable AsientoContable { get; set; } = null!;
}
