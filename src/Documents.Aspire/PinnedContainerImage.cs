namespace Documents.Aspire;

/// <summary>
/// A pinned container image resolved from <c>container-images.json</c>.
/// </summary>
/// <param name="Registry">Registry hostname (e.g. <c>docker.io</c>, <c>quay.io</c>).</param>
/// <param name="Image">Image repository as expected by Aspire's <c>WithImage(...)</c> — without registry and without tag.</param>
/// <param name="Tag">Pinned tag (e.g. <c>17-alpine</c>).</param>
public sealed record PinnedContainerImage(string Registry, string Image, string Tag)
{
    /// <summary>Default Docker Hub registry hostname.</summary>
    public const string DockerHubRegistry = "docker.io";

    /// <summary>
    /// <see langword="true"/> when this image is hosted on Docker Hub. Aspire validators
    /// (e.g. RabbitMQ <c>WithManagementPlugin</c>) reject an explicit <c>docker.io</c>
    /// registry, so callers must skip <c>WithImageRegistry</c> in that case.
    /// </summary>
    public bool IsDockerHub => string.Equals(Registry, DockerHubRegistry, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Fully qualified reference suitable for <c>docker pull</c>.
    /// </summary>
    /// <remarks>
    /// For <c>docker.io</c> the registry prefix is omitted because Docker Hub auto-resolves
    /// the implicit <c>library/</c> namespace only when the registry is not stated explicitly.
    /// </remarks>
    public string FullReference => IsDockerHub ? $"{Image}:{Tag}" : $"{Registry}/{Image}:{Tag}";
}
