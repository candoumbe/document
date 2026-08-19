using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Documents.DataStores;

/// <summary>
/// Stores and removes document content outside the relational database.
/// </summary>
public interface IDocumentContentStorage
{
    /// <summary>
    /// Stores content and returns the key used to address it.
    /// </summary>
    Task<string> StoreAsync(Stream content, long size, string contentType, string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes previously stored content.
    /// </summary>
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);
}