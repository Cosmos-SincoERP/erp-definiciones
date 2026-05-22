using Cosmos.DatosReferencia.Consultas.Monedas.Exceptions;
using Cosmos.DatosReferencia.Consultas.Monedas.Queries;
using Cosmos.DatosReferencia.Consultas.Monedas.ReadModels;
using Cosmos.DatosReferencia.Dominio.Compartidos.Excepciones;
using Cosmos.DatosReferencia.Dominio.Compartidos.Store;
using Cosmos.DatosReferencia.Dominio.Monedas;
using Cosmos.EventSourcing.Abstractions.Queries;

namespace Cosmos.DatosReferencia.Consultas.Monedas.QueryHandlers;

public class ConsultarMonedaPorCodigoHandler(IDomainStore domainStore)
    : IQueryHandler<MonedaQueries.ConsultarPorCodigo, MonedaReadModel>
{
    public async Task<MonedaReadModel> HandleAsync(
        MonedaQueries.ConsultarPorCodigo query, CancellationToken cancellationToken)
    {
        var moneda = await domainStore.FirstOrDefaultAsync<Moneda>(
            moneda => moneda.Codigo.Valor == query.Codigo, cancellationToken)
            ?? throw new ConsultarMonedaPorCodigoException(DomainExceptionType.NotFound,
                $"No se encontró una moneda con código '{query.Codigo}'.");

        return new MonedaReadModel
        {
            Codigo = moneda.Codigo.Valor,
            Nombre = moneda.Nombre,
            Decimales = moneda.Decimales,
            Activo = moneda.Activo
        };
    }
}
