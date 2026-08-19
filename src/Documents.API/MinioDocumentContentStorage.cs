using Documents.DataStores;
using Minio;
using Minio.DataModel.Args;

namespace Documents.API;

/// <summary>
/// Stores document content in the MinIO S3-compatible object store.
/// </summary>
public sealed class MinioDocumentContentStorage : IDocumentContentStorage
{
    private const string BucketName = "documents";
    private readonly IMinioClient _client;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private bool _bucketEnsured;

    /// <summary>
    /// Builds a storage client from the Aspire-provided MinIO connection string.
    /// </summary>
    public MinioDocumentContentStorage(IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("minio")
            ?? throw new InvalidOperationException("The minio connection string is required.");
        Dictionary<string, string> connection = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(part => part.Length == 2)
            .ToDictionary(part => part[0], part => part[1], StringComparer.OrdinalIgnoreCase);

        // Aspire's MinIO resource emits "Endpoint=http://host:port;AccessKey=...;SecretKey=...", not Host/Port/Username/Password.
        string endpoint = connection.GetValueOrDefault("Endpoint")
            ?? throw new InvalidOperationException("The minio connection string must contain an Endpoint.");
        string accessKey = connection.GetValueOrDefault("AccessKey")
            ?? throw new InvalidOperationException("The minio connection string must contain an AccessKey.");
        string secretKey = connection.GetValueOrDefault("SecretKey")
            ?? throw new InvalidOperationException("The minio connection string must contain a SecretKey.");

        var endpointUri = new Uri(endpoint, UriKind.Absolute);

        _client = new MinioClient()
            .WithEndpoint(endpointUri.Host, endpointUri.Port)
            .WithCredentials(accessKey, secretKey)
            .Build();
    }

    /// <inheritdoc />
    public async Task<string> StoreAsync(Stream content, long size, string contentType, string objectKey, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken).ConfigureAwait(false);
        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectKey)
            .WithStreamData(content)
            .WithObjectSize(size)
            .WithContentType(contentType), cancellationToken).ConfigureAwait(false);
        return objectKey;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default) =>
        _client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(BucketName).WithObject(objectKey), cancellationToken);

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketEnsured)
        {
            return;
        }

        await _bucketLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_bucketEnsured)
            {
                return;
            }

            bool exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(BucketName), cancellationToken).ConfigureAwait(false);
            if (!exists)
            {
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(BucketName), cancellationToken).ConfigureAwait(false);
            }

            _bucketEnsured = true;
        }
        finally
        {
            _bucketLock.Release();
        }
    }
}