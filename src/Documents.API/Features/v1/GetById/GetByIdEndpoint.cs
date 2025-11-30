using Candoumbe.DataAccess.Abstractions;
using Documents.Ids;
using Documents.Objects;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Optional;

namespace Documents.API.Features.v1.GetById;

/// <summary>
/// Gets a document by its identifier.
/// </summary>
public class GetByIdEndpoint : Endpoint<DocumentId, Results<Ok<DocumentInfo>, NotFound>>
{
    private readonly IUnitOfWorkFactory _uowFactory;

    /// <summary>
    /// Builds a new <see cref="GetByIdEndpoint"/> instance.
    /// </summary>
    /// <param name="unitOfWorkFactory"></param>
    public GetByIdEndpoint(IUnitOfWorkFactory unitOfWorkFactory)
    {
        _uowFactory = unitOfWorkFactory;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Verbs(Http.GET, Http.HEAD);
        Routes("/documents/{id:guid}");
        Version(1);
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task<Results<Ok<DocumentInfo>, NotFound>> ExecuteAsync(DocumentId req, CancellationToken ct)
    {
        using IUnitOfWork uow = _uowFactory.NewUnitOfWork();
        SelectSpecification<Document, DocumentInfo> selector = new(doc => new DocumentInfo
        {
            Name = doc.Name,
            Id = doc.Id,
            MimeType = doc.MimeType,
            Hash = doc.Hash,
            CreatedAt = doc.CreatedDate,
            UpdatedAt = doc.UpdatedDate
        });
        FilterSpecification<DocumentInfo> filter = new(doc => doc.Id == req);
        Option<DocumentInfo> entity = await uow.Repository<Document>().SingleOrDefault(selector, filter, ct);

        return entity.Match<Results<Ok<DocumentInfo>, NotFound>>(
            some: info => TypedResults.Ok(info),
            none: () => TypedResults.NotFound());
    }
}