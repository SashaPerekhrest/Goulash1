using Portfolio.Application.DTOs.Files;

namespace Portfolio.Application.Interfaces;

public interface IFileStorageService
{
    Task<FileUploadResponse> UploadAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        long size,
        string? folder,
        CancellationToken cancellationToken);
}
