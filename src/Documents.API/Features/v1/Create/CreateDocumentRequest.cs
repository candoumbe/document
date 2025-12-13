using Documents.Ids;

namespace Documents.API.Features.v1.Create;

/// <summary>
/// Request to create a document.
/// </summary>
public record CreateDocumentRequest
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
    /// Mime type of the document.
    /// </summary>
    public string MimeType { get; init; }

    /// <summary>
    /// Content of the document.
    /// </summary>
    public byte[] Content { get; init; }
}