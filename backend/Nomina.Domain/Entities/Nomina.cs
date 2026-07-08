using Nomina.Domain.Enums;

namespace Nomina.Domain.Entities;

public class Nomina
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;

    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
}
