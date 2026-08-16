#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Documents.Aspire;

/// <summary>
/// Pinned container images loaded from the embedded <c>container-images.json</c> manifest.
/// </summary>
/// <remarks>
/// The JSON manifest is the single source of truth — it is shared verbatim with the build
/// pipeline (which pre-pulls every image before integration tests) and with the architectural
/// tests (which guarantee that every image used by the AppHost is declared here, and vice versa).
/// </remarks>
public static partial class ContainerImages
{
    /// <summary>Logical key for the PostgreSQL image, used by the <c>postgres</c> resource.</summary>
    public const string PostgresKey = "postgres";

    /// <summary>Logical key for the RabbitMQ image, used by the <c>messaging</c> resource.</summary>
    public const string RabbitMqKey = "rabbitmq";

    /// <summary>Logical key for the Keycloak image, used by the <c>keycloak</c> resource.</summary>
    public const string KeycloakKey = "keycloak";

    /// <summary>
    /// Logical key for the PgAdmin image, used by the <c>pgadmin</c> resource.
    /// </summary>
    public const string PgAdminKey = "pgadmin";

    /// <summary>Manifest resource name relative to the assembly (logical name).</summary>
    public const string ManifestResourceName = "Documents.AppHost.container-images.json";

    private static readonly IReadOnlyDictionary<string, PinnedContainerImage> s_images = LoadFromAssembly(typeof(ContainerImages).Assembly);

    /// <summary>PostgreSQL image used by the <c>postgres</c> resource.</summary>
    public static PinnedContainerImage Postgres => s_images[PostgresKey];

    /// <summary>RabbitMQ image used by the <c>messaging</c> resource.</summary>
    public static PinnedContainerImage RabbitMq => s_images[RabbitMqKey];

    /// <summary>Keycloak image used by the <c>keycloak</c> resource.</summary>
    public static PinnedContainerImage Keycloak => s_images[KeycloakKey];

    /// <summary>PgAdmin image used by the <c>pgadmin</c> resource.</summary>
    public static PinnedContainerImage PgAdmin => s_images[PgAdminKey];

    /// <summary>All declared images, keyed by their logical name.</summary>
    public static IReadOnlyDictionary<string, PinnedContainerImage> All => s_images;

    /// <summary>
    /// Loads the manifest from the given assembly. Exposed for tests and for the build pipeline,
    /// which embeds the same JSON file under the same logical resource name.
    /// </summary>
    public static IReadOnlyDictionary<string, PinnedContainerImage> LoadFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        using Stream stream = assembly.GetManifestResourceStream(ManifestResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ManifestResourceName}' was not found in assembly '{assembly.FullName}'. " +
                "The container-images.json manifest must be embedded as a resource.");

        return Parse(stream);
    }

    /// <summary>
    /// Parses a manifest stream. Exposed so tests can validate arbitrary content.
    /// </summary>
    public static IReadOnlyDictionary<string, PinnedContainerImage> Parse(Stream manifestStream)
    {
        ArgumentNullException.ThrowIfNull(manifestStream);

        ContainerImagesManifest? manifest = JsonSerializer.Deserialize(manifestStream, ContainerImagesJsonContext.Default.ContainerImagesManifest)
            ?? throw new InvalidOperationException("Container images manifest deserialized to null.");

        if (manifest.Images is null || manifest.Images.Count == 0)
        {
            throw new InvalidOperationException("Container images manifest is empty.");
        }

        Dictionary<string, PinnedContainerImage> result = new(manifest.Images.Count, StringComparer.Ordinal);
        foreach (KeyValuePair<string, ContainerImageManifestEntry> entry in manifest.Images)
        {
            string key = entry.Key;
            ContainerImageManifestEntry value = entry.Value;

            if (string.IsNullOrWhiteSpace(value.Registry) || string.IsNullOrWhiteSpace(value.Image) || string.IsNullOrWhiteSpace(value.Tag))
            {
                throw new InvalidOperationException(
                    $"Container image manifest entry '{key}' is incomplete: registry, image and tag are all required.");
            }

            result[key] = new PinnedContainerImage(value.Registry!, value.Image!, value.Tag!);
        }

        return result;
    }

    internal sealed class ContainerImagesManifest
    {
        [JsonPropertyName("images")]
        public Dictionary<string, ContainerImageManifestEntry> Images { get; set; } = [];
    }

    internal sealed class ContainerImageManifestEntry
    {
        [JsonPropertyName("registry")]
        public string? Registry { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("tag")]
        public string? Tag { get; set; }
    }

    [JsonSerializable(typeof(ContainerImagesManifest))]
    internal partial class ContainerImagesJsonContext : JsonSerializerContext
    {
    }
}
