using AwesomeAssertions;
using Cosmos.DatosReferencia.Dominio.Compartidos.Excepciones;
using Cosmos.DatosReferencia.Dominio.Monedas;
using Cosmos.DatosReferencia.Dominio.Monedas.CommandHandlers;
using Cosmos.DatosReferencia.Dominio.Monedas.Commands;
using Cosmos.DatosReferencia.Dominio.Monedas.Exceptions;
using Cosmos.DatosReferencia.Dominio.Monedas.ValueObjects;
using Cosmos.DatosReferencia.Dominio.Tests.Compartidos.Imitaciones;

namespace Cosmos.DatosReferencia.Dominio.Tests.Monedas.Comandos;

public class AgregarMonedaTests
{
    private readonly TestDomainStore _domainStore = new();
    private AgregarMonedaHandler Handler => new(_domainStore);

    [Fact]
    public async Task Si_MonedaNoExisteYDatosSonValidos_Debe_PersistirMoneda()
    {
        await Handler.HandleAsync(
            new MonedaCommands.Agregar("USD", "Dólar estadounidense", 2),
            TestContext.Current.CancellationToken);

        var persistida = await _domainStore.FirstOrDefaultAsync<Moneda>(
            moneda => moneda.Codigo.Valor == "USD",
            TestContext.Current.CancellationToken);

        persistida.Should().BeEquivalentTo(
            new Moneda(new CodigoMoneda("USD"), "Dólar estadounidense", 2));
    }

    [Fact]
    public async Task Si_MonedaYaExisteConElMismoCodigo_Debe_LanzarExcepcionBusinessRule()
    {
        await Handler.HandleAsync(
            new MonedaCommands.Agregar("USD", "Dólar estadounidense", 2),
            TestContext.Current.CancellationToken);

        var caller = () => Handler.HandleAsync(
            new MonedaCommands.Agregar("USD", "Otra denominación", 2),
            TestContext.Current.CancellationToken);

        await caller.Should().ThrowAsync<AgregarMonedaException>()
            .Where(excepcion => excepcion.Type == DomainExceptionType.BusinessRule)
            .WithMessage("*'USD'*");
    }

    [Fact]
    public async Task Si_CodigoEsInvalidoISO4217_Debe_LanzarExcepcionInvalidData()
    {
        var caller = () => Handler.HandleAsync(
            new MonedaCommands.Agregar("usd", "Dólar estadounidense", 2),
            TestContext.Current.CancellationToken);

        await caller.Should().ThrowAsync<MonedaException>()
            .Where(excepcion => excepcion.Type == DomainExceptionType.InvalidData)
            .WithMessage("*'usd'*ISO 4217*");
    }
}
