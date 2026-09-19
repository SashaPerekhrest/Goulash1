namespace Portfolio.Application.DTOs.Auth;

public sealed record AuthUserDto(
    Guid Id,
    string Login);
