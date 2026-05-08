using AwesomeAssertions;
using Cosmos.DatosReferencia.Dominio.Compartidos.Excepciones;
using Cosmos.DatosReferencia.Dominio.Monedas.Exceptions;
using Cosmos.DatosReferencia.Dominio.Monedas.ValueObjects;

namespace Cosmos.DatosReferencia.Dominio.Tests.Monedas.ValueObjects;

public class CodigoMonedaTests
{
    [Fact]
    public void Si_CodigoEstaEnMinusculas_Debe_LanzarExcepcionInvalidData()
    {
        var caller = () => new CodigoMoneda("usd");

        caller.Should().Throw<MonedaException>()
            .Where(excepcion => excepcion.Type == DomainExceptionType.InvalidData)
            .WithMessage("*'usd'*");
    }

    [Fact]
    public void Si_CodigoTieneDosLetras_Debe_LanzarExcepcionInvalidData()
    {
        var caller = () => new CodigoMoneda("US");

        caller.Should().Throw<MonedaException>()
            .Where(excepcion => excepcion.Type == DomainExceptionType.InvalidData)
            .WithMessage("*'US'*");
    }

    [Fact]
    public void Si_CodigoTieneCuatroLetras_Debe_LanzarExcepcionInvalidData()
    {
        var caller = () => new CodigoMoneda("USDX");

        caller.Should().Throw<MonedaException>()
            .Where(excepcion => excepcion.Type == DomainExceptionType.InvalidData)
            .WithMessage("*'USDX'*");
    }

    [Fact]
    public void Si_CodigoEsNuloOVacio_Debe_LanzarExcepcionInvalidData()
    {
        var conNulo = () => new CodigoMoneda(null!);
        var conVacio = () => new CodigoMoneda("");

        conNulo.Should().Throw<MonedaException>()
            .Where(excepcion => excepcion.Type == DomainExceptionType.InvalidData)
            .WithMessage("*nulo o vacío*");
        conVacio.Should().Throw<MonedaException>()
            .Where(excepcion => excepcion.Type == DomainExceptionType.InvalidData)
            .WithMessage("*nulo o vacío*");
    }

    [Fact]
    public void Si_CodigoEsTresLetrasMayusculas_Debe_ConstruirCodigo()
    {
        var codigo = new CodigoMoneda("COP");

        codigo.Valor.Should().Be("COP");
    }
}
