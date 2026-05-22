using AwesomeAssertions;
using Cosmos.DatosReferencia.Consultas.Monedas.Exceptions;
using Cosmos.DatosReferencia.Consultas.Monedas.Queries;
using Cosmos.DatosReferencia.Consultas.Monedas.QueryHandlers;
using Cosmos.DatosReferencia.Consultas.Monedas.ReadModels;
using Cosmos.DatosReferencia.Dominio.Compartidos.Excepciones;
using Cosmos.DatosReferencia.Dominio.Monedas;
using Cosmos.DatosReferencia.Dominio.Monedas.ValueObjects;
using Cosmos.DatosReferencia.Dominio.Tests.Compartidos.Imitaciones;

namespace Cosmos.DatosReferencia.Dominio.Tests.Monedas.Consultas;

public class ConsultarMonedaPorCodigoTests
{
    private readonly TestDomainStore _domainStore = new();
    private ConsultarMonedaPorCodigoHandler Handler => new(_domainStore);

    [Fact]
    public async Task Si_MonedaExiste_Debe_RetornarMonedaConSusAtributos()
    {
        await _domainStore.SaveAsync(
            new Moneda(new CodigoMoneda("USD"), "Dólar estadounidense", 2),
            TestContext.Current.CancellationToken);

        var resultado = await Handler.HandleAsync(
            new MonedaQueries.ConsultarPorCodigo("USD"),
            TestContext.Current.CancellationToken);

        resultado.Should().BeEquivalentTo(new MonedaReadModel
        {
            Codigo = "USD",
            Nombre = "Dólar estadounidense",
            Decimales = 2,
            Activo = true
        });
    }

    [Fact]
    public async Task Si_MonedaNoExiste_Debe_LanzarExcepcionNotFound()
    {
        var caller = () => Handler.HandleAsync(
            new MonedaQueries.ConsultarPorCodigo("USD"),
            TestContext.Current.CancellationToken);

        await caller.Should().ThrowAsync<ConsultarMonedaPorCodigoException>()
            .Where(excepcion => excepcion.Type == DomainExceptionType.NotFound)
            .WithMessage("*'USD'*");
    }
}
