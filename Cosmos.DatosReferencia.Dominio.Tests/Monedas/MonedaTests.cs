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

    [Fact]
    public void Si_NombreYDecimalesSonValidos_Debe_RetornarMonedaConValoresActualizados()
    {
        var original = new Moneda(new CodigoMoneda("COP"), "Peso colombiano", 2);

        var modificada = original.Modificar("Peso colombiano (nuevo)", 0);

        modificada.Codigo.Valor.Should().Be("COP");
        modificada.Nombre.Should().Be("Peso colombiano (nuevo)");
        modificada.Decimales.Should().Be(0);
        modificada.Activo.Should().BeTrue();
    }

    [Fact]
    public void Si_NuevoNombreEsNuloOVacio_Debe_LanzarExcepcionInvalidData()
    {
        var original = new Moneda(new CodigoMoneda("COP"), "Peso colombiano", 2);

        var conNulo = () => original.Modificar(null!, 2);
        var conVacio = () => original.Modificar("", 2);

        conNulo.Should().Throw<MonedaException>()
            .Where(excepcion => excepcion.Type == DomainExceptionType.InvalidData)
            .WithMessage("*nombre*");
        conVacio.Should().Throw<MonedaException>()
            .Where(excepcion => excepcion.Type == DomainExceptionType.InvalidData)
            .WithMessage("*nombre*");
    }

    [Fact]
    public void Si_NuevosDecimalesSonNegativos_Debe_LanzarExcepcionInvalidData()
    {
        var original = new Moneda(new CodigoMoneda("COP"), "Peso colombiano", 2);

        var caller = () => original.Modificar("Peso colombiano", -1);

        caller.Should().Throw<MonedaException>()
            .Where(excepcion => excepcion.Type == DomainExceptionType.InvalidData)
            .WithMessage("*-1*");
    }

    [Fact]
    public void Si_MonedaEsInactiva_Debe_MantenerseInactivaTrasModificar()
    {
        var inactiva = new Moneda(new CodigoMoneda("COP"), "Peso colombiano", 2) { Activo = false };

        var modificada = inactiva.Modificar("Otro nombre", 0);

        modificada.Activo.Should().BeFalse();
    }
}
