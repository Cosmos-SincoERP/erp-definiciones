using AwesomeAssertions;
using Cosmos.DatosReferencia.Dominio.Compartidos.Excepciones;
using Cosmos.DatosReferencia.Dominio.Monedas;
using Cosmos.DatosReferencia.Dominio.Monedas.Exceptions;
using Cosmos.DatosReferencia.Dominio.Monedas.ValueObjects;

namespace Cosmos.DatosReferencia.Dominio.Tests.Monedas;

public class MonedaTests
{
    [Fact]
    public void Si_DatosDeMonedaSonValidos_Debe_ConstruirMoneda()
    {
        var moneda = new Moneda(new CodigoMoneda("COP"), "Peso colombiano", 2);

        moneda.Codigo.Valor.Should().Be("COP");
        moneda.Nombre.Should().Be("Peso colombiano");
        moneda.Decimales.Should().Be(2);
        moneda.Activo.Should().BeTrue();
    }

    [Fact]
    public void Si_DecimalesEsNegativo_Debe_LanzarExcepcionInvalidData()
    {
        var caller = () => new Moneda(new CodigoMoneda("COP"), "Peso colombiano", -1);

        caller.Should().Throw<MonedaException>()
            .Where(excepcion => excepcion.Type == DomainExceptionType.InvalidData)
            .WithMessage("*-1*");
    }

    [Fact]
    public void Si_NombreEsNuloOVacio_Debe_LanzarExcepcionInvalidData()
    {
        var conNulo = () => new Moneda(new CodigoMoneda("COP"), null!, 2);
        var conVacio = () => new Moneda(new CodigoMoneda("COP"), "", 2);

        conNulo.Should().Throw<MonedaException>()
            .Where(excepcion => excepcion.Type == DomainExceptionType.InvalidData)
            .WithMessage("*nombre*");
        conVacio.Should().Throw<MonedaException>()
            .Where(excepcion => excepcion.Type == DomainExceptionType.InvalidData)
            .WithMessage("*nombre*");
    }
}
