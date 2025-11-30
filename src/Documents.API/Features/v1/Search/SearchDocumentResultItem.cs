using Candoumbe.Types.Numerics;
using Documents.Ids;
using NodaTime;

namespace Documents.API.Features.v1.Search;

/// <summary>
/// Result item of a document search.
/// </summary>
public record SearchDocumentResultItem
{
    /// <summary>
    /// Id of the document
    /// </summary>
    public required DocumentId Id { get; init; }

    /// <summary>
    /// Name of the document
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Mime type of the document
    /// </summary>
    public string MimeType { get; set; }

    /// <summary>
    /// Size of the document (in bytes)
    /// </summary>
    public required NonNegativeLong Size { get; set; }

    /// <summary>
    /// SHA256 hash of the document
    /// </summary>
    public required string Hash { get; init; }

    /// <summary>
    /// Date and time when the document was created.
    /// </summary>
    public Instant? CreatedAt {get; init;}

    /// <summary>
    /// Date and time when the document was last updated.
    /// </summary>
    public Instant? LastUpdatedAt {get; init;}
}