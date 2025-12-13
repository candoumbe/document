namespace Documents.API.Features.v1.List;

/// <summary>
/// Request to list all documents.
/// </summary>
public sealed record ListDocumentsRequest : AbstractSearchRequest<DocumentInfo>;