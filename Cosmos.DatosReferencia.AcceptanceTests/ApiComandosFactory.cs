using Cosmos.DatosReferencia.Comandos.API;
using Cosmos.EventSourcing.CritterStack.Commands;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Wolverine.Marten;

namespace Cosmos.DatosReferencia.AcceptanceTests;

public class ApiComandosFactory(string npgCnn, string rabbitCnn) : WebApplicationFactory<IApiComandosAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(config =>
        {
            var overrideSettings = new Dictionary<string, string>
            {
                ["ConnectionStrings:RabbitMQ"] = rabbitCnn
            };
            config.AddInMemoryCollection(overrideSettings!);
        });

        builder.UseEnvironment("Tests");
        builder.ConfigureTestServices(services =>
            services.AgregarConfiguracionMartenComandos(npgCnn, "cosmos.datosreferencia", (_) => { }, true)
                .IntegrateWithWolverine());
    }
}