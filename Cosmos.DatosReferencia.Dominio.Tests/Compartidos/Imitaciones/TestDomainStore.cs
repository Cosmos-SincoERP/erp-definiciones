using System.Linq.Expressions;
using Cosmos.DatosReferencia.Dominio.Compartidos.Store;

namespace Cosmos.DatosReferencia.Dominio.Tests.Compartidos.Imitaciones;

internal class TestDomainStore : IDomainStore
{
    private List<object> Documentos { get; } = [];

    public Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : notnull
        => Task.FromResult(Documentos.OfType<T>().Any(predicate.Compile()));

    public Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : notnull
        => Task.FromResult(Documentos.OfType<T>().FirstOrDefault(predicate.Compile()));

    public Task SaveAsync<T>(T modelo, CancellationToken ct = default) where T : notnull
    {
        Documentos.Add(modelo);
        return Task.CompletedTask;
    }
}
