using StronglyTypedIds;

namespace Documents.Ids;

/// <summary>
/// A strongly typed appointment identifier.
/// </summary>
[StronglyTypedId("guid-v7", "guid-efcore")]
// ReSharper disable once StructCanBeMadeReadOnly
public partial struct DocumentId;