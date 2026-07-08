using Nomina.Domain.Entities;

namespace Nomina.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> FindActiveByEmailWithRolAsync(string email, CancellationToken ct = default);
}
