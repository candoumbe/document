using Candoumbe.DataAccess.Abstractions;
using Documents.Ids;
using Documents.Objects;
using FastEndpoints;
using FastEndpoints.AspVersioning;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Documents.API.Features.v1.Delete;

/// <summary>
/// Deletes a document by its identifier.
/// </summary>
public class DeleteByIdEndpoint : Endpoint<DeleteByIdRequest, Results<NotFound, NoContent>>
{
    private readonly IUnitOfWorkFactory _uowFactory;

    /// <summary>
    /// Builds a new <see cref="DeleteByIdEndpoint"/> instance.
    /// </summary>
    /// <param name="uowFactory"></param>
    public DeleteByIdEndpoint(IUnitOfWorkFactory uowFactory)
    {
        _uowFactory = uowFactory;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/documents/{id:guid}");
        Options(x => x.WithVersionSet("documents").MapToApiVersion(1.0));
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task<Results<NotFound, NoContent>> ExecuteAsync(DeleteByIdRequest req, CancellationToken ct)
    {
        using IUnitOfWork uow = _uowFactory.NewUnitOfWork();
        FilterSpecification<Document> filter = new(x => x.Id == req.Id);

        await uow.Repository<Document>().Delete(filter, ct);
        await uow.SaveChangesAsync(ct)
            .ConfigureAwait(false);

        return TypedResults.NoContent();
    }
}