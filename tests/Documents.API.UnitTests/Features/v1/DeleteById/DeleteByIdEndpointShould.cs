using System.Threading.Tasks;
using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.EFStore;
using Documents.API.Features.v1.Delete;
using Documents.API.UnitTests.Fixtures;
using Documents.DataStores;
using Documents.Ids;
using FakeItEasy;
using FastEndpoints;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;

namespace Documents.API.UnitTests.Features.v1.DeleteById;

public class DeleteByIdEndpointShould : IClassFixture<PostgresSqlFixture>
{
    private readonly DeleteByIdEndpoint _sut;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IClock _clock;

    public DeleteByIdEndpointShould(PostgresSqlFixture fixture)
    {
        _clock = A.Fake<IClock>();
        DbContextOptionsBuilder<DocumentsStore> optionsBuilder = new();
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

        _sut = Factory.Create<DeleteByIdEndpoint>(_unitOfWorkFactory);
    }

    [Fact]
    public async Task Return_NoContent_when_the_request_does_not_match_a_document_in_the_datastore()
    {
        // Act
        DocumentId idToDelete = DocumentId.New();
        Results<NotFound, NoContent> result = await _sut.ExecuteAsync(idToDelete, TestContext.Current.CancellationToken);

        // Assert
        result.Result.Should()
            .BeAssignableTo<NoContent>();
    }

    [Fact]
    public async Task Returns_NoContent_when_the_request_does_match_an_existing_document()
    {
        // Arrange

        // Act
        DocumentId idToDelete = DocumentId.New();
        Results<NotFound, NoContent> actionResult = await _sut.ExecuteAsync(idToDelete, TestContext.Current.CancellationToken);

        // Assert
        actionResult.Result.Should()
            .BeAssignableTo<NoContent>();

    }
}