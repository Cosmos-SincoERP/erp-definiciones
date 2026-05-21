namespace Cosmos.DatosReferencia.Dominio.Monedas.Commands;

public abstract record MonedaCommands
{
    private MonedaCommands() { }

    public record Agregar(string Codigo, string Nombre, int Decimales) : MonedaCommands;
}
