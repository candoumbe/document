using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bogus;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.EFStore;
using Candoumbe.Types.Numerics;
using Documents.API.Features;
using Documents.API.Features.v1;
using Documents.API.Features.v1.GetById;
using Documents.API.UnitTests.Fixtures;
using Documents.DataStores;
using Documents.Ids;
using Documents.Objects;
using FakeItEasy;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;
using Xunit.OpenCategories.V3;

namespace Documents.API.UnitTests.Features.v1.GetById;

[UnitTest]
public class GetByIdEndpointShould : IClassFixture<PostgresSqlFixture>
{
    private static readonly Faker s_faker = new();
    private readonly GetByIdEndpoint _sut;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory ;
    private readonly LinkGenerator _linkGenerator;
    private readonly IClock _clock;

    public GetByIdEndpointShould(PostgresSqlFixture fixture)
    {
        _clock = A.Fake<IClock>();
        DbContextOptionsBuilder<DocumentsStore> optionsBuilder = new DbContextOptionsBuilder<DocumentsStore>();
        optionsBuilder.UseNpgsql(fixture.ConnectionString, options => options.UseNodaTime()
            .EnableRetryOnFailure(3));

        _unitOfWorkFactory = new EntityFrameworkUnitOfWorkFactory<DocumentsStore>(optionsBuilder.Options,
            options =>
            {
                DocumentsStore store = new (options, _clock);
                store.Database.EnsureCreated();
                return store;
            },
            new DocumentRepositoryFactory());



        _linkGenerator = A.Fake<LinkGenerator>();
        A.CallTo(() => _linkGenerator.GetUriByAddress(A<HttpContext>.Ignored,
                A<string>.Ignored,
                A<RouteValueDictionary>.Ignored,
                A<RouteValueDictionary>.Ignored,
                A<string>.Ignored,
                A<HostString>.Ignored,
                A<PathString>.Ignored,
                A<FragmentString>.Ignored,
                A<LinkOptions>.Ignored))
            .WithAnyArguments()
            .Returns(s_faker.Internet.Url());
        _sut = Factory.Create<GetByIdEndpoint>(_unitOfWorkFactory, _linkGenerator);
    }

    [Fact]
    public async Task Returns_Ok_when_the_request_matches_an_existing_document()
    {
        // Arrange
        Faker faker = new();
        DocumentId documentId = DocumentId.New();

        DocumentPart[] parts =
        [
            new (documentId, 0, $"{documentId}/0", 10),
            new (documentId, 1, $"{documentId}/1", 10),
            new (documentId, 2, $"{documentId}/2", 10)
        ];

        Document entry = new(id: documentId, name: "the batman in action", mimeType: "image/mpeg4");
        entry.UpdateSize(NonNegativeLong.From(parts.Sum(p => p.Size)));
        entry.UpdateHash(s_faker.Random.Hash());
        entry.Lock();

        using (IUnitOfWork uow = _unitOfWorkFactory.NewUnitOfWork())
        {
            await uow.Repository<Document>().Create(entry, TestContext.Current.CancellationToken);
            await uow.SaveChangesAsync(TestContext.Current.CancellationToken);
        }


        // Act
        Results<Ok<Browsable<DocumentInfo>>, NotFound> result = await _sut.ExecuteAsync(documentId, ct: TestContext.Current.CancellationToken);

        // Assert
        result.Result.Should().BeAssignableTo<Ok<Browsable<DocumentInfo>>>();
        Ok<Browsable<DocumentInfo>> okResult = result.Result.As<Ok<Browsable<DocumentInfo>>>();
        Browsable<DocumentInfo> browsable = okResult.Value;
        browsable.Resource.Id.Should().Be(documentId);
    }

    [Fact]
    public async Task Returns_NotFound_when_the_request_does_not_match_an_existing_document()
    {
        // Act
        Results<Ok<Browsable<DocumentInfo>>, NotFound> result = await _sut.ExecuteAsync(DocumentId.New(), TestContext.Current.CancellationToken);

        // Assert
        result.Result.Should().BeAssignableTo<NotFound>();
    }

}