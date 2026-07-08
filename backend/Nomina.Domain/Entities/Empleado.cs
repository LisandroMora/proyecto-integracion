using Nomina.Domain.Enums;

namespace Nomina.Domain.Entities;

public class Empleado
{
    public int Id { get; set; }
    public string Cedula { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Departamento { get; set; }
    public string? Puesto { get; set; }
    public decimal SalarioMensual { get; set; }
    public int NominaId { get; set; }
    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;

    public Nomina Nomina { get; set; } = null!;
    public ICollection<Transaccion> Transacciones { get; set; } = new List<Transaccion>();
}
