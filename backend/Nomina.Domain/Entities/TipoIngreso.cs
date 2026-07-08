using Nomina.Domain.Enums;

namespace Nomina.Domain.Entities;

public class TipoIngreso
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool DependeDeSalario { get; set; }
    public decimal? Porcentaje { get; set; }
    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
}
