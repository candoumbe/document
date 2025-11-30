using Documents.Ids;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Documents.API.Features.v1.Update;

/// <summary>
/// Endpoint to update a document
/// </summary>
public class UpdateDocumentByItsIdEndpoint : Endpoint<DocumentId, Results<NotFound, Conflict, NoContent>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Put("/documents/{id:guid}");
        Version(1);
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override Task<Results<NotFound, Conflict, NoContent>> ExecuteAsync(DocumentId req, CancellationToken ct)
    {
        return Task.FromResult<Results<NotFound, Conflict, NoContent>>(TypedResults.NoContent());
    }
}