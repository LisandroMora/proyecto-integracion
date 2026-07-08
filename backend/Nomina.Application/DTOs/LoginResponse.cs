namespace Nomina.Application.DTOs;

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    string Email,
    string Rol);
