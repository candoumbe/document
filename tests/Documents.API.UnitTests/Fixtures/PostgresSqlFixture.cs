using System.Threading.Tasks;
using Bogus;
using Testcontainers.PostgreSql;
using Xunit;

namespace Documents.API.UnitTests.Fixtures;

/// <summary>
/// A test fixture that provides a postgres database.
/// </summary>
public class PostgresSqlFixture : IAsyncLifetime
{

    private readonly PostgreSqlContainer _container;
    private static readonly Faker s_faker = new();

    /// <summary>
    /// Connection string to the postgres database.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Builds a new <see cref="PostgresSqlFixture"/> instance.
    /// </summary>
    public PostgresSqlFixture()
    {
        // configure the postgres container
        _container = new PostgreSqlBuilder()
            .WithUsername(s_faker.Internet.UserName())
            .WithPassword(s_faker.Internet.Password())
            .Build();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }
}