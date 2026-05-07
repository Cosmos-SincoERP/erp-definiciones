using Marten;

namespace Cosmos.DatosReferencia.Comandos.API;

public static class ProyeccionesRegister
{
    public static void AgregarProyeccionesEnLinea(this IServiceCollection services)
    {
        services.ConfigureMarten(options => { });
    }
}