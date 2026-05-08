using System.Text.RegularExpressions;
using Cosmos.DatosReferencia.Dominio.Compartidos.Excepciones;
using Cosmos.DatosReferencia.Dominio.Monedas.Exceptions;

namespace Cosmos.DatosReferencia.Dominio.Monedas.ValueObjects;

public record CodigoMoneda(string Valor)
{
    private static readonly Regex Iso4217 = new("^[A-Z]{3}$", RegexOptions.Compiled);

    public string Valor { get; } = ValidarValor(Valor);

    private static string ValidarValor(string valor) =>
        string.IsNullOrWhiteSpace(valor)
            ? throw new MonedaException(DomainExceptionType.InvalidData,
                "El código de moneda no puede ser nulo o vacío.")
            : !Iso4217.IsMatch(valor)
                ? throw new MonedaException(DomainExceptionType.InvalidData,
                    $"El código de moneda ('{valor}') debe ser ISO 4217 — exactamente 3 letras mayúsculas.")
                : valor;
}
