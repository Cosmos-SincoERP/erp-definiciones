using Cosmos.DatosReferencia.Dominio.Compartidos.Excepciones;
using Cosmos.DatosReferencia.Dominio.Compartidos.Store;
using Cosmos.DatosReferencia.Dominio.Monedas.Commands;
using Cosmos.DatosReferencia.Dominio.Monedas.Exceptions;
using Cosmos.DatosReferencia.Dominio.Monedas.ValueObjects;
using Cosmos.EventSourcing.Abstractions.Commands;

namespace Cosmos.DatosReferencia.Dominio.Monedas.CommandHandlers;

public class AgregarMonedaHandler(IDomainStore domainStore)
    : ICommandHandlerAsync<MonedaCommands.Agregar>
{
    public async Task HandleAsync(MonedaCommands.Agregar command, CancellationToken cancellationToken)
    {
        var codigo = new CodigoMoneda(command.Codigo);

        if (await domainStore.AnyAsync<Moneda>(moneda => moneda.Codigo.Valor == codigo.Valor, cancellationToken))
            throw new AgregarMonedaException(DomainExceptionType.BusinessRule,
                $"Ya existe una moneda con código '{codigo.Valor}'.");

        var moneda = new Moneda(codigo, command.Nombre, command.Decimales);
        await domainStore.SaveAsync(moneda, cancellationToken);
    }
}
