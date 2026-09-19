namespace Portfolio.Application.DTOs.Common;

public sealed record ErrorResponse(
    string Message,
    IDictionary<string, string[]>? Errors = null);
