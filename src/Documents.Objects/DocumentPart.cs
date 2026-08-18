using System;
using Documents.Ids;

namespace Documents.Objects;

/// <summary>
/// A chunk of a larger document.
/// </summary>
public class DocumentPart
{
    /// <summary>
    /// Object key of the document part in the dedicated file storage.
    /// </summary>
    public string ObjectKey { get; }

    /// <summary>
    /// Position of the content amongst its siblings
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Identifier of the <see cref="Document"/> which this content is attached to.
    /// </summary>
    public DocumentId DocumentId { get; }

    /// <summary>
    /// Size of the content in bytes
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// Builds a new <see cref="DocumentPart"/>
    /// </summary>
    /// <param name="documentId">id of the <see cref="Document"/> which this content is attached to</param>
    /// <param name="position">O-based index of the position of the current instance amongst all other<see cref="DocumentPart"/>s for a same <see cref="Document"/>.</param>
    /// <param name="objectKey">Object key in the dedicated file storage</param>
    /// <param name="size">Size of the stored content in bytes</param>
    public DocumentPart(DocumentId documentId, int position, string objectKey, long size)
    {
        if (objectKey is null)
        {
            throw new ArgumentNullException(nameof(objectKey));
        }

        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new ArgumentException($"{nameof(objectKey)} cannot be empty", nameof(objectKey));
        }

        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, $"{nameof(size)} must be greater than zero");
        }

        if (position < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "position must be a 0-based index");
        }

        if (documentId == DocumentId.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(documentId), documentId, $"{nameof(documentId)} cannot be empty");
        }

        DocumentId = documentId;
        ObjectKey = objectKey;
        Position = position;
        Size = size;
    }
}