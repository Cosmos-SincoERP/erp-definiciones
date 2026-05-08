using Cosmos.DatosReferencia.Dominio.Compartidos.Excepciones;
using Cosmos.DatosReferencia.Dominio.Monedas.Exceptions;
using Cosmos.DatosReferencia.Dominio.Monedas.ValueObjects;

namespace Cosmos.DatosReferencia.Dominio.Monedas;

public record Moneda(CodigoMoneda Codigo, string Nombre, int Decimales)
{
    public string Nombre { get; } = string.IsNullOrWhiteSpace(Nombre)
        ? throw new MonedaException(DomainExceptionType.InvalidData,
            "El nombre de la moneda no puede ser nulo o vacío.")
        : Nombre.Trim();

    public int Decimales { get; } = Decimales < 0
        ? throw new MonedaException(DomainExceptionType.InvalidData,
            $"El número de decimales ({Decimales}) no puede ser negativo.")
        : Decimales;

    public bool Activo { get; init; } = true;
}
