using Documents.Ids;

namespace Documents.API.Features.v1.GetById;


/// <summary>
/// Request to retrieve a document
/// </summary>
/// <param name="Id">Identifier of the document to retrieve</param>
public record GetByIdRequest(DocumentId Id);