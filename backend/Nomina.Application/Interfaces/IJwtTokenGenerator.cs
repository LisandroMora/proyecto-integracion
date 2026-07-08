using Nomina.Domain.Entities;

namespace Nomina.Application.Interfaces;

public record JwtToken(string Value, DateTime ExpiresAt);

public interface IJwtTokenGenerator
{
    JwtToken Generate(Usuario usuario);
}
