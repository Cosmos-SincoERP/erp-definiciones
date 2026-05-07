using Cosmos.DatosReferencia.Consultas.API;
using Cosmos.EventSourcing.CritterStack.Queries;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Wolverine.Marten;

namespace Cosmos.DatosReferencia.AcceptanceTests;

public class ApiConsultasFactory(string npgCnn) : WebApplicationFactory<IApiConsultasAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Tests");
        builder.ConfigureTestServices(services =>
            services.AgregarConfiguracionMartenConsultas(npgCnn, "cosmos.datosreferencia", true,
                    ProyeccionesRegister.AgregarProyecciones)
                .IntegrateWithWolverine());
    }
}