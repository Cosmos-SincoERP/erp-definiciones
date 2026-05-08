namespace Cosmos.DatosReferencia.Dominio.Compartidos.Excepciones;

public enum DomainExceptionType
{
    BusinessRule,
    NotFound,
    InvalidData
}

public abstract class DomainException : Exception
{
    public DomainExceptionType Type { get; }

    protected DomainException(DomainExceptionType type, string message)
        : base(message)
    {
        Type = type;
    }
}
