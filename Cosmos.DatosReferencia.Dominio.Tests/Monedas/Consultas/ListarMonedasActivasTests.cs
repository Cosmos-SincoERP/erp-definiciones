using AwesomeAssertions;
using Cosmos.DatosReferencia.Consultas.Monedas.Queries;
using Cosmos.DatosReferencia.Consultas.Monedas.QueryHandlers;
using Cosmos.DatosReferencia.Dominio.Monedas;
using Cosmos.DatosReferencia.Dominio.Monedas.ValueObjects;
using Cosmos.DatosReferencia.Dominio.Tests.Compartidos.Imitaciones;

namespace Cosmos.DatosReferencia.Dominio.Tests.Monedas.Consultas;

public class ListarMonedasActivasTests
{
    private readonly TestDomainStore _domainStore = new();
    private ListarMonedasActivasHandler Handler => new(_domainStore);

    [Fact]
    public async Task Si_HayMonedasActivasEInactivas_Debe_RetornarSoloActivas()
    {
        var activaUsd = new Moneda(new CodigoMoneda("USD"), "Dólar estadounidense", 2);
        var activaEur = new Moneda(new CodigoMoneda("EUR"), "Euro", 2);
        var inactivaCop = new Moneda(new CodigoMoneda("COP"), "Peso colombiano", 2) { Activo = false };
        await _domainStore.SaveAsync(activaUsd, TestContext.Current.CancellationToken);
        await _domainStore.SaveAsync(activaEur, TestContext.Current.CancellationToken);
        await _domainStore.SaveAsync(inactivaCop, TestContext.Current.CancellationToken);

        var resultado = await Handler.HandleAsync(
            new MonedaQueries.ListarActivas(),
            TestContext.Current.CancellationToken);

        resultado.Select(moneda => moneda.Codigo.Valor)
            .Should().BeEquivalentTo(["USD", "EUR"]);
    }

    [Fact]
    public async Task Si_NoHayMonedas_Debe_RetornarListaVacia()
    {
        var resultado = await Handler.HandleAsync(
            new MonedaQueries.ListarActivas(),
            TestContext.Current.CancellationToken);

        resultado.Should().BeEmpty();
    }
}
