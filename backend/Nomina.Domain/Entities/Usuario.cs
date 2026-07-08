using Nomina.Domain.Enums;

namespace Nomina.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RolId { get; set; }
    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;

    public Rol Rol { get; set; } = null!;
}
