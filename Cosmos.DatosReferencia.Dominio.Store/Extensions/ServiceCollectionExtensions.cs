using Cosmos.DatosReferencia.Dominio.Compartidos.Store;
using Microsoft.Extensions.DependencyInjection;

namespace Cosmos.DatosReferencia.Dominio.Store.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public void AgregarDomainStore()
        {
            services.AddScoped<IDomainStore, DomainStore>();
        }
    }
}
