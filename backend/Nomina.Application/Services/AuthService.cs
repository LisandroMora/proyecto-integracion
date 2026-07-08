using Nomina.Application.DTOs;
using Nomina.Application.Interfaces;
using Nomina.Domain.Enums;

namespace Nomina.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;

    public AuthService(IUsuarioRepository usuarios, IPasswordHasher hasher, IJwtTokenGenerator jwt)
    {
        _usuarios = usuarios;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var usuario = await _usuarios.FindActiveByEmailWithRolAsync(request.Email, ct);
        if (usuario is null || usuario.Estado != EstadoRegistro.Activo) return null;

        if (!_hasher.Verify(request.Password, usuario.PasswordHash)) return null;

        var token = _jwt.Generate(usuario);
        return new LoginResponse(token.Value, token.ExpiresAt, usuario.Email, usuario.Rol.Nombre);
    }
}
