using System.Net;
using System.Text.RegularExpressions;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Portfolio.Application.DTOs.Files;
using Portfolio.Application.Interfaces;

namespace Portfolio.Infrastructure.Storage;

public sealed partial class S3FileStorageService(
    IAmazonS3 s3Client,
    IOptions<StorageOptions> options) : IFileStorageService
{
    private readonly StorageOptions options = options.Value;

    public async Task<FileUploadResponse> UploadAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        long size,
        string? folder,
        CancellationToken cancellationToken)
    {
        ValidateOptions();
        await EnsureBucketExistsAsync(cancellationToken);

        var objectKey = BuildObjectKey(originalFileName, contentType, folder);

        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = options.ResolvedBucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = contentType,
            AutoCloseStream = false
        }, cancellationToken);

        return new FileUploadResponse(
            Path.GetFileName(objectKey),
            contentType,
            size,
            BuildPublicUrl(objectKey));
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await s3Client.HeadBucketAsync(new HeadBucketRequest
            {
                BucketName = options.ResolvedBucketName
            }, cancellationToken);
            return;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            if (!options.CreateBucketIfNotExists)
            {
                throw new InvalidOperationException(
                    $"Storage bucket '{options.ResolvedBucketName}' does not exist.",
                    exception);
            }
        }

        try
        {
            await s3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = options.ResolvedBucketName
            }, cancellationToken);
        }
        catch (AmazonS3Exception exception)
            when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            // Another process created the bucket between the HEAD and PUT requests.
        }
    }

    private string BuildPublicUrl(string objectKey)
    {
        return $"{options.PublicBaseUrl.TrimEnd('/')}/{EncodeObjectKey(objectKey)}";
    }

    private static string BuildObjectKey(string originalFileName, string contentType, string? folder)
    {
        var extension = NormalizeExtension(Path.GetExtension(originalFileName), contentType);
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        var safeBaseName = Slugify(baseName);
        var generatedFileName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}-{safeBaseName}{extension}";
        var safeFolder = NormalizeFolder(folder);

        return safeFolder is null
            ? generatedFileName
            : $"{safeFolder}/{generatedFileName}";
    }

    private static string? NormalizeFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return null;
        }

        var segments = folder
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Slugify)
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();

        return segments.Length == 0 ? null : string.Join('/', segments);
    }

    private static string Slugify(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "file";
        }

        var lower = value.Trim().ToLowerInvariant();
        var slug = UnsafeObjectNameCharacters().Replace(lower, "-").Trim('-');
        slug = RepeatedHyphenRegex().Replace(slug, "-");

        if (slug.Length == 0)
        {
            return "file";
        }

        return slug.Length <= 80 ? slug : slug[..80].Trim('-');
    }

    private static string NormalizeExtension(string extension, string contentType)
    {
        if (!string.IsNullOrWhiteSpace(extension))
        {
            var safeExtension = ExtensionCharacters().Replace(extension.ToLowerInvariant(), string.Empty);
            if (safeExtension.Length is > 1 and <= 12)
            {
                return safeExtension;
            }
        }

        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            _ => ".bin"
        };
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint)
            || string.IsNullOrWhiteSpace(options.ResolvedBucketName)
            || string.IsNullOrWhiteSpace(options.PublicBaseUrl)
            || string.IsNullOrWhiteSpace(options.Region))
        {
            throw new InvalidOperationException("Storage settings are not configured.");
        }
    }

    private static string EncodeObjectKey(string objectKey)
    {
        return string.Join('/', objectKey.Split('/').Select(EncodePathSegment));
    }

    private static string EncodePathSegment(string value)
    {
        return Uri.EscapeDataString(value).Replace("%2F", "/", StringComparison.Ordinal);
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex UnsafeObjectNameCharacters();

    [GeneratedRegex("-+")]
    private static partial Regex RepeatedHyphenRegex();

    [GeneratedRegex("[^a-z0-9.]")]
    private static partial Regex ExtensionCharacters();
}
