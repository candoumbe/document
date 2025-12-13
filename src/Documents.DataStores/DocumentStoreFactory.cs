using Candoumbe.DataAccess.Abstractions;
using Candoumbe.DataAccess.EFStore;

namespace Documents.DataStores;

/// <summary>
/// Factory for <see cref="DocumentsStore"/>
/// </summary>
public class DocumentRepositoryFactory : IRepositoryFactory<DocumentsStore>
{
    /// <inheritdoc />
    public IRepository<TEntity> NewRepository<TEntity>(DocumentsStore dbContext) where TEntity : class
    {
        return new EntityFrameworkRepository<TEntity, DocumentsStore>(dbContext);
    }
}