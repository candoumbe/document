using Documents.Ids;

namespace Documents.API.Features.v1.Delete;


/// <summary>
/// Request to delete a document
/// </summary>
/// <param name="Id">Identifier of the document to delete</param>
public record DeleteByIdRequest(DocumentId Id);