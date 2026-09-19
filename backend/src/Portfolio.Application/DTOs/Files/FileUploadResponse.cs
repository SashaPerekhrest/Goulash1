namespace Portfolio.Application.DTOs.Files;

public sealed record FileUploadResponse(
    string FileName,
    string ContentType,
    long Size,
    string Url);
