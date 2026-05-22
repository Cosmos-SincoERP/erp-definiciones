namespace Cosmos.DatosReferencia.Consultas.Monedas.ReadModels;

public record MonedaReadModel
{
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public int Decimales { get; init; }
    public bool Activo { get; init; }
}
