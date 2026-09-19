namespace Portfolio.Application.DTOs.Auth;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    AuthUserDto User);
