using Cosmos.DatosReferencia.Dominio.Compartidos.Excepciones;

namespace Cosmos.DatosReferencia.Consultas.Monedas.Exceptions;

public class ConsultarMonedaPorCodigoException(DomainExceptionType tipo, string mensaje)
    : DomainException(tipo, mensaje);
