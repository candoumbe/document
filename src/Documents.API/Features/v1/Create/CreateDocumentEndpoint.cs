using System.Security.Cryptography;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.Forms;
using Candoumbe.Types.Numerics;
using Documents.API.Features.v1.GetById;
using Documents.Ids;
using Documents.Objects;
using FastEndpoints;
using FastEndpoints.AspVersioning;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Documents.API.Features.v1.Create
{
    /// <summary>
    /// Endpoint to create a document.
    /// </summary>
    public class CreateDocumentEndpoint : Endpoint<CreateDocumentRequest, Results<Conflict, Created<Browsable<DocumentInfo>>>>
    {
        private readonly IUnitOfWorkFactory _unitOfWorkFactory;
        private readonly LinkGenerator _linkGenerator;

        /// <summary>
        /// Builds a new <see cref="CreateDocumentEndpoint"/> instance.
        /// </summary>
        /// <param name="unitOfWorkFactory"></param>
        /// <param name="linkGenerator"></param>
        public CreateDocumentEndpoint(IUnitOfWorkFactory unitOfWorkFactory, LinkGenerator linkGenerator)
        {
            _unitOfWorkFactory = unitOfWorkFactory;
            _linkGenerator = linkGenerator;
        }

        /// <inheritdoc />
        public override void Configure()
        {
            Post("/documents");
            Options(x => x.WithVersionSet("documents").MapToApiVersion(1.0));
            AllowAnonymous();
        }

        /// <inheritdoc />
        public override async Task<Results<Conflict, Created<Browsable<DocumentInfo>>>> ExecuteAsync(CreateDocumentRequest req, CancellationToken ct)
        {
            using IUnitOfWork uow = _unitOfWorkFactory.NewUnitOfWork();
            DocumentId documentId = req.Id == DocumentId.Empty
                ? DocumentId.New()
                : req.Id;

            Document document = new(id: documentId, name: req.Name);

            if (!string.IsNullOrWhiteSpace(req.MimeType))
            {
                document.ChangeMimeTypeTo(req.MimeType);
            }

            await uow.Repository<Document>().Create(document, ct);
            await uow.Repository<DocumentPart>().Create(new DocumentPart(document.Id, 0, req.Content), ct);

            document.UpdateSize(NonNegativeLong.From(req.Content.Length));
            document.UpdateHash(BitConverter.ToString(SHA256.Create().ComputeHash(req.Content)));
            document.Lock();

            await uow.SaveChangesAsync(ct).ConfigureAwait(false);

            return TypedResults.Created((Uri)null,
                new Browsable<DocumentInfo>
                {
                    Resource = new DocumentInfo()
                    {
                        MimeType = document.MimeType,
                        CreatedAt = document.CreatedDate,
                        Hash = document.Hash,
                        Id = document.Id,
                        Name = document.Name,
                        UpdatedAt = document.UpdatedDate
                    },
                    Links =
                    [
                        new Link { Href= _linkGenerator.GetUriByName(HttpContext, IEndpoint.GetName<GetByIdEndpoint>(Http.GET), new { document.Id }), Relations = [LinkRelation.Self]}
                    ]
                });
        }
    }
}