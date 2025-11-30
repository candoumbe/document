using Candoumbe.Types.Numerics;
using Documents.Ids;
using NodaTime;

namespace Documents.API.Features.v1;

/// <summary>
/// Information about a document.
/// </summary>
public record DocumentInfo
{
    /// <summary>
    /// Unique identifier of the document.
    /// </summary>
    public DocumentId Id { get; init; }

    /// <summary>
    /// Name of the document.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Name of the document.
    /// </summary>
    public string MimeType { get; init; }

    /// <summary>
    /// Sha256 hash of the document.
    /// </summary>
    public string Hash { get; init; }

    /// <summary>
    /// Size of the document (in bytes).
    /// </summary>
    public NonNegativeLong Size { get; init; }

    /// <summary>
    /// When was the document created?
    /// </summary>
    public Instant? CreatedAt { get; init; }

    /// <summary>
    /// When was the document last updated?
    /// </summary>
    public Instant? UpdatedAt { get; init; }
}