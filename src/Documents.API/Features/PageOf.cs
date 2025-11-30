namespace Documents.API.Features;

/// <summary>
/// Wraps a page
/// </summary>
/// <typeparam name="TResource">Type of the resource the page will contain</typeparam>
public class PageOf<TResource> where TResource : class
{
    /// <summary>
    /// Index of the page
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Indicates the number of elements the current page is a subset of
    /// </summary>
    public long Total { get; init; }

    /// <summary>
    /// The number of elements the current page holds
    /// </summary>
    public long Count { get; init; }

    /// <summary>
    /// Resources hold in the current page
    /// </summary>
    public IEnumerable<TResource> Items
    {
        get => _items;
        set => _items = value ?? Enumerable.Empty<TResource>();
    }

    private IEnumerable<TResource> _items;

    /// <summary>
    /// Navigation links between pages result
    /// </summary>
    public PageLinks Links { get; init; }

    /// <summary>
    /// Builds an empty page
    /// </summary>
    public PageOf()
    {
        _items = new List<TResource>();
    }
}