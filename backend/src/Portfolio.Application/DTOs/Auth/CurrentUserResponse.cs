namespace Portfolio.Application.DTOs.Auth;

public sealed record CurrentUserResponse(
    Guid Id,
    string Login);
