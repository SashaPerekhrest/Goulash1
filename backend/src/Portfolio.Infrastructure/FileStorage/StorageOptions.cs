namespace Portfolio.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Endpoint { get; init; } = string.Empty;

    public string AccessKey { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public string BucketName { get; init; } = string.Empty;

    public string Bucket { get; init; } = string.Empty;

    public string PublicBaseUrl { get; init; } = string.Empty;

    public bool ForcePathStyle { get; init; } = true;

    public bool CreateBucketIfNotExists { get; init; } = true;

    public string Region { get; init; } = "us-east-1";

    public string ResolvedBucketName => string.IsNullOrWhiteSpace(BucketName)
        ? Bucket
        : BucketName;
}
