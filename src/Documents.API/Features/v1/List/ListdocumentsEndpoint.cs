using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.Repositories;
using Candoumbe.Forms;
using Documents.API.Features.v1.Search;
using Documents.Objects;
using FastEndpoints;
using FastEndpoints.AspVersioning;

namespace Documents.API.Features.v1.List;

/// <summary>
/// Endpoint to list documents.
/// </summary>
public class ListdocumentsEndpoint : Endpoint<ListDocumentsRequest, PageOf<Browsable<DocumentInfo>>>
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly LinkGenerator _urlHelper;

    /// <summary>
    /// Builds a new <see cref="ListdocumentsEndpoint"/> instance.
    /// </summary>
    /// <param name="uowFactory"></param>
    /// <param name="urlHelper"></param>
    public ListdocumentsEndpoint(IUnitOfWorkFactory uowFactory, LinkGenerator urlHelper)
    {
        _uowFactory = uowFactory;
        _urlHelper = urlHelper;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Verbs(Http.GET, Http.HEAD);
        Options(x => x.WithVersionSet("documents").MapToApiVersion(1.0));
        Routes("/documents");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task<PageOf<Browsable<DocumentInfo>>> ExecuteAsync(ListDocumentsRequest req, CancellationToken ct)
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

        MultiOrderSpecification<DocumentInfo> orderBy = new(
        [
            (x => x.UpdatedAt, OrderDirection.Descending),
            (x => x.CreatedAt, OrderDirection.Descending)
        ]);

        Page<DocumentInfo> page = await uow.Repository<Document>()
            .ReadPage(selector,
                pageSize: PageSize.From(req.PageSize),
                page: PageIndex.From(req.Page),
                orderBy,
                ct);

        bool hasNextPage = req.Page < page.Count;

        return new PageOf<Browsable<DocumentInfo>>
        {
            Items =
            [
                .. page.Entries
                    .Select(x => new Browsable<DocumentInfo>
                    {
                        Resource = x,
                        Links =
                        [
                            new Link { Href = _urlHelper.GetPathByName(IEndpoint.GetName<GetById.GetByIdEndpoint>(), new { id = x.Id }) }
                        ]
                    })
            ],
            Links = new PageLinks(
                First: new Link
                {
                    Href = _urlHelper.GetPathByName(IEndpoint.GetName<SearchDocumentsEndpoint>(),
                        new { page = 1, page.Size })
                },
                Next: hasNextPage
                    ? new Link
                    {
                        Href = _urlHelper.GetPathByName(IEndpoint.GetName<SearchDocumentsEndpoint>(),
                            new { page = req.Page + 1, req.PageSize })
                    }
                    : null,
                Previous: req.Page > 1
                    ? new Link
                    {
                        Href = _urlHelper.GetPathByName(IEndpoint.GetName<SearchDocumentsEndpoint>(),
                            new { page = req.Page - 1, req.PageSize })
                    }
                    : null,
                Last: new Link
                {
                    Href = _urlHelper.GetPathByName(IEndpoint.GetName<SearchDocumentsEndpoint>(),
                        new { page = page.Count, req.PageSize, })
                }),
            Total = page.Total,
            Count = page.Count,
            Page = req.Page
        };
    }
}