using Candoumbe.Types.Numerics;

namespace Documents.API.Features.v1;

/// <summary>
/// Base class for search requests
/// </summary>
public abstract record AbstractSearchRequest<T>
{
    /// <summary>
    /// Index of the page
    /// </summary>
    public NonNegativeInteger Page { get; init; }

    /// <summary>
    /// Defines the number of items the result set will contain at most.
    /// </summary>
    /// <remarks>
    /// This value is just a hint that the server may not fulfill. The server may return fewer items depending on its configuration.
    /// </remarks>
    public PositiveInteger PageSize { get; init; }

    /// <summary>
    /// Directive on how to sort results
    /// </summary>
    public string Sort { get; init; }
}