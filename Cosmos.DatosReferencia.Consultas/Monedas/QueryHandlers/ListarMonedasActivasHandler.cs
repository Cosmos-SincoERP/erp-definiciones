using Cosmos.DatosReferencia.Consultas.Monedas.Queries;
using Cosmos.DatosReferencia.Dominio.Compartidos.Store;
using Cosmos.DatosReferencia.Dominio.Monedas;
using Cosmos.EventSourcing.Abstractions.Queries;

namespace Cosmos.DatosReferencia.Consultas.Monedas.QueryHandlers;

public class ListarMonedasActivasHandler(IDomainStore domainStore)
    : IQueryHandler<MonedaQueries.ListarActivas, IReadOnlyList<Moneda>>
{
    public Task<IReadOnlyList<Moneda>> HandleAsync(
        MonedaQueries.ListarActivas query, CancellationToken cancellationToken)
        => domainStore.WhereAsync<Moneda>(moneda => moneda.Activo, cancellationToken);
}
