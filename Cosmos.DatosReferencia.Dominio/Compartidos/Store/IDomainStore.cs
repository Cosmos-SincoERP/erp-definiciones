using System.Linq.Expressions;

namespace Cosmos.DatosReferencia.Dominio.Compartidos.Store;

public interface IDomainStore
{
    Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : notnull;

    Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        where T : notnull;

    Task SaveAsync<T>(T modelo, CancellationToken ct = default) where T : notnull;
}
