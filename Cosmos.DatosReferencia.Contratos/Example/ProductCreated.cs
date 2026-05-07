using Cosmos.EventDriven.Abstractions;

namespace Cosmos.DatosReferencia.Contratos.Example;

public record ProductCreated(string Name, string Description, decimal Price): IPublicEvent;