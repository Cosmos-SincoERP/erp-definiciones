using Cosmos.DatosReferencia.Consultas.API;
using JasperFx.Events;
using JasperFx.Events.Daemon;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Cosmos.DatosReferencia.Proyecciones.Tests;

public class HostTestFactory : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresSqlContainer = new PostgreSqlBuilder().Build();
    private IHost _host = null!;

    public IDocumentStore DocumentStore => _host.Services.GetRequiredService<IDocumentStore>();

    public async ValueTask InitializeAsync()
    {
        await _postgresSqlContainer.StartAsync();
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddMarten(options =>
                {
                    options.Events.StreamIdentity = StreamIdentity.AsString;
                    options.Connection(_postgresSqlContainer.GetConnectionString());
                    options.Projections.AgregarProyecciones();
                }).AddAsyncDaemon(DaemonMode.HotCold);
            }).Build();

        await _host.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _postgresSqlContainer.DisposeAsync().AsTask();
    }
}