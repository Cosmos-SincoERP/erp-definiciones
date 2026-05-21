using System.Linq.Expressions;
using Cosmos.DatosReferencia.Dominio.Compartidos.Store;
using Marten;

namespace Cosmos.DatosReferencia.Dominio.Store;

public class DomainStore(IDocumentSession session) : IDomainStore
{
    public Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return session.Query<T>().AnyAsync(predicate, ct);
    }

    public Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return session.Query<T>().FirstOrDefaultAsync(predicate, ct);
    }

    public async Task SaveAsync<T>(T modelo, CancellationToken ct = default) where T : notnull
    {
        session.Store(modelo);
        await session.SaveChangesAsync(ct);
    }
}
