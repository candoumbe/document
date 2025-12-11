using System.Linq.Expressions;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.Repositories;
using Candoumbe.Forms;
using DataFilters;
using Documents.API.Features.v1.GetById;
using Documents.Objects;
using FastEndpoints;
using Microsoft.Extensions.Options;
using OrderDirection = Candoumbe.DataAccess.Abstractions.OrderDirection;

namespace Documents.API.Features.v1.Search;

/// <summary>
/// Endpoint to search documents.
/// </summary>
public class SearchDocumentsEndpoint : Endpoint<SearchDocumentQuery, PageOf<Browsable<SearchDocumentResultItem>>>
{
    private readonly IOptionsSnapshot<DocumentsApiOptions> _apiOptions;
    private readonly LinkGenerator _urlHelper;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    /// <summary>
    /// Builds a new <see cref="SearchDocumentsEndpoint"/> instance.
    /// </summary>
    /// <param name="apiOptions">Api options</param>
    /// <param name="urlHelper">helper to generate links.</param>
    /// <param name="unitOfWorkFactory">Factory to create <see cref="IUnitOfWork"/> instances.</param>
    public SearchDocumentsEndpoint(IOptionsSnapshot<DocumentsApiOptions> apiOptions,
        LinkGenerator urlHelper,
        IUnitOfWorkFactory unitOfWorkFactory)
    {
        _apiOptions = apiOptions;
        _urlHelper = urlHelper;
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Verbs(Http.GET, Http.HEAD);
        Routes("/documents/search");
        Version(1, deprecateAt: 2);
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task<PageOf<Browsable<SearchDocumentResultItem>>> ExecuteAsync(SearchDocumentQuery search, CancellationToken ct)
    {
        search = search with { PageSize = search.PageSize < _apiOptions.Value.MaxPageSize ? search.PageSize : _apiOptions.Value.MaxPageSize };

        IFilterSpecification<Document> filterSpecification = ComputeFilterSpecification(search);
        IProjectionSpecification<Document, SearchDocumentResultItem> selector = ComputeProjectionSelectSpecification();
        IOrderSpecification<SearchDocumentResultItem> orderSpecification = ComputeOrderBySpecification(search.Sort);

        using IUnitOfWork uow = _unitOfWorkFactory.NewUnitOfWork();

        Page<SearchDocumentResultItem> searchResult = await uow.Repository<Document>() .Where(selector,
                                                                                               filterSpecification,
                                                                                               orderSpecification,
                                                                                               PageSize.From(search.PageSize),
                                                                                               PageIndex.From(search.Page),
                                                                                               ct);

        bool hasNextPage = search.Page < searchResult.Count;

        return new PageOf<Browsable<SearchDocumentResultItem>>
        {
            Items = searchResult.Entries
                .Select(x => new Browsable<SearchDocumentResultItem>
                {
                    Resource = x,
                    Links = new[]
                    {
                        new Link
                        {
                            Relations = [LinkRelation.Self],
                            Method = "GET",
                            Href = _urlHelper.GetPathByName(IEndpoint.GetName<GetByIdEndpoint>(),
                                new { x.Id })
                        }
                    }
                }),
            Links = new PageLinks(
                First: new Link
                {
                    Href = _urlHelper.GetPathByName(IEndpoint.GetName<SearchDocumentsEndpoint>(),
                        new
                        {
                            page = 1,
                            search.PageSize,
                            search.Name,
                            search.Sort,
                            search.MimeType
                        })
                },
                Next: hasNextPage
                    ? new Link
                    {
                        Href = _urlHelper.GetPathByName(IEndpoint.GetName<SearchDocumentsEndpoint>(),
                            new
                            {
                                page = search.Page + 1,
                                search.PageSize,
                                search.Name,
                                search.MimeType,
                                search.Sort
                            })
                    }
                    : null,
                Previous: search.Page > 1
                    ? new Link
                    {
                        Href = _urlHelper.GetPathByName(IEndpoint.GetName<SearchDocumentsEndpoint>(),
                            new
                            {
                                page = search.Page - 1,
                                search.PageSize,
                                search.Name,
                                search.MimeType,
                                search.Sort
                            })
                    }
                    : null,
                Last: new Link
                {
                    Href = _urlHelper.GetPathByName(IEndpoint.GetName<SearchDocumentsEndpoint>(),
                        new
                        {
                            page = searchResult.Count,
                            search.PageSize,
                            search.Name,
                            search.MimeType,
                            search.Sort
                        })
                }),
            Total = searchResult.Total,
            Count = searchResult.Count,
            Page = search.Page
        };


        static IProjectionSpecification<Document, SearchDocumentResultItem> ComputeProjectionSelectSpecification()
        {
            return new SelectSpecification<Document, SearchDocumentResultItem>(entity => new SearchDocumentResultItem()
            {
                Hash = entity.Hash,
                MimeType = entity.MimeType,
                Name = entity.Name,
                Id = entity.Id,
                CreatedAt = entity.CreatedDate,
                LastUpdatedAt = entity.UpdatedDate,
                Size = entity.Size
            });
        }

        static IFilterSpecification<Document> ComputeFilterSpecification(SearchDocumentQuery req)
        {
            List<IFilter> filters = [];

            if (!string.IsNullOrWhiteSpace(req.Name))
            {
                filters.Add($"{nameof(DocumentInfo.Name)}={req.Name}".ToFilter<DocumentInfo>());
            }

            if (!string.IsNullOrWhiteSpace(req.MimeType))
            {
                filters.Add($"{nameof(DocumentInfo.MimeType)}={req.MimeType}".ToFilter<DocumentInfo>());
            }

            IFilter filter = filters switch
            {
                { Count: > 0 } => new MultiFilter { Logic = FilterLogic.And, Filters = filters },
                _ => Filter.True
            };
            Expression<Func<Document, bool>> expression = filter.ToExpression<Document>();

            return new FilterSpecification<Document>(expression);
        }

        static IOrderSpecification<SearchDocumentResultItem> ComputeOrderBySpecification(string searchSort)
        {
            return new MultiOrderSpecification<SearchDocumentResultItem>(
            [
                (x => x.LastUpdatedAt, OrderDirection.Descending),
                (x => x.Name, OrderDirection.Ascending)
            ]);
        }

    }
}