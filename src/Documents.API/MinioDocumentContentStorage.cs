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

        string host = connection.GetValueOrDefault("Host")
            ?? throw new InvalidOperationException("The minio connection string must contain a Host.");
        int port = connection.TryGetValue("Port", out string portValue) && int.TryParse(portValue, out int parsedPort) ? parsedPort : 9000;
        string username = connection.GetValueOrDefault("Username")
            ?? throw new InvalidOperationException("The minio connection string must contain a Username.");
        string password = connection.GetValueOrDefault("Password")
            ?? throw new InvalidOperationException("The minio connection string must contain a Password.");

        _client = new MinioClient()
            .WithEndpoint(host, port)
            .WithCredentials(username, password)
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