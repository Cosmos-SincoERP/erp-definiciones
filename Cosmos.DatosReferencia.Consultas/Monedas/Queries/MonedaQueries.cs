namespace Cosmos.DatosReferencia.Consultas.Monedas.Queries;

public abstract record MonedaQueries
{
    private MonedaQueries() { }

    public record ConsultarPorCodigo(string Codigo) : MonedaQueries;
    public record ListarActivas() : MonedaQueries;
}
