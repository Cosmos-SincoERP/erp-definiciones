using Cosmos.DatosReferencia.Dominio.Compartidos.Excepciones;

namespace Cosmos.DatosReferencia.Dominio.Monedas.Exceptions;

public class AgregarMonedaException(DomainExceptionType tipo, string mensaje)
    : DomainException(tipo, mensaje);
