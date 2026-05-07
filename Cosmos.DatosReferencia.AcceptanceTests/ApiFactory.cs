using Marten;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Cosmos.DatosReferencia.AcceptanceTests;

public class ApiFactory : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresSqlContainer = new PostgreSqlBuilder().Build();
    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder().Build();
    public ApiComandosFactory CommandsFactory { get; private set; } = null!;
    public ApiConsultasFactory QueriesFactory { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgresSqlContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();

        var npgCnn = _postgresSqlContainer.GetConnectionString();

        CommandsFactory = new ApiComandosFactory(npgCnn,
            _rabbitMqContainer.GetConnectionString());
        QueriesFactory = new ApiConsultasFactory(npgCnn);
    }

    public async ValueTask DisposeAsync()
    {
        await _postgresSqlContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
        // await CommandsFactory.DisposeAsync();
        // await QueriesFacotry.DisposeAsync();
    }
}

/// <summary>
/// Esta colleción de pruebas se utiliza para agrupar las pruebas de aceptación. Pero hay que tener cuidado porque esto implica compartir el mismo contenedor de pruebas para todas las pruebas de aceptación.
/// </summary>
[CollectionDefinition("AcceptanceTests")]
public class AcceptanceTestsCollection : ICollectionFixture<ApiFactory>;

[Collection("AcceptanceTests")]
public class AcceptanceTest
{
    public AcceptanceTest(ApiFactory factory)
    {
        var documentStore = factory.CommandsFactory.Services.GetRequiredService<IDocumentStore>();
        documentStore.Advanced.ResetAllData().GetAwaiter().GetResult();
    }
}