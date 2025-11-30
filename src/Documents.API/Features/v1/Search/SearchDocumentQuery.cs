namespace Documents.API.Features.v1.Search;

/// <summary>
/// Query to search documents.
/// </summary>
public record SearchDocumentQuery : AbstractSearchRequest<DocumentInfo>
{
    /// <summary>
    /// Name of the document to search.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Mime type of the document to search.
    /// </summary>
    public string MimeType { get; init; }
}