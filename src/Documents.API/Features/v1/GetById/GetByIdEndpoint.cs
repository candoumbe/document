using Candoumbe.DataAccess.Abstractions;
using Candoumbe.Forms;
using Documents.Ids;
using Documents.Objects;
using FastEndpoints;
using FastEndpoints.AspVersioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Optional;

namespace Documents.API.Features.v1.GetById;

/// <summary>
/// Gets a document by its identifier.
/// </summary>
public class GetByIdEndpoint : Endpoint<DocumentId, Results<Ok<Browsable<DocumentInfo>>, NotFound>>
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly LinkGenerator _linkGenerator;

    /// <summary>
    /// Builds a new <see cref="GetByIdEndpoint"/> instance.
    /// </summary>
    /// <param name="unitOfWorkFactory"></param>
    /// <param name="linkGenerator"></param>
    public GetByIdEndpoint(IUnitOfWorkFactory unitOfWorkFactory, LinkGenerator linkGenerator)
    {
        _uowFactory = unitOfWorkFactory;
        _linkGenerator = linkGenerator;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Verbs(Http.GET, Http.HEAD);
        Routes("/documents/{id:guid}");
        Options(x => x.WithVersionSet("documents").MapToApiVersion(1.0));
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task<Results<Ok<Browsable<DocumentInfo>>, NotFound>> ExecuteAsync(DocumentId req, CancellationToken ct)
    {
        using IUnitOfWork uow = _uowFactory.NewUnitOfWork();
        SelectSpecification<Document, DocumentInfo> selector = new(doc => new DocumentInfo
        {
            Name = doc.Name,
            Id = doc.Id,
            MimeType = doc.MimeType,
            Hash = doc.Hash,
            Size = doc.Size,
            CreatedAt = doc.CreatedDate,
            UpdatedAt = doc.UpdatedDate
        });
        FilterSpecification<DocumentInfo> filter = new(doc => doc.Id == req);
        Option<DocumentInfo> entity = await uow.Repository<Document>().SingleOrDefault(selector, filter, ct);

        return entity.Match<Results<Ok<Browsable<DocumentInfo>>, NotFound>>(
            some: info =>
            {
                Browsable<DocumentInfo> browsable = new()
                {
                    Resource = info,
                    Links = [
                        new Link()
                        {
                            Relations = [LinkRelation.Self], Href = _linkGenerator.GetPathByName(HttpContext!, IEndpoint.GetName<GetByIdEndpoint>(), new { info.Id })
                        }
                    ]
                };
                return TypedResults.Ok(browsable);
            },
            none: () => TypedResults.NotFound());
    }
}