using WhisperLive.Abstraction;
using WhisperLive.Abstraction.Models;

namespace WhisperLive.Client.Implementation;

/// <summary>
/// Represents a segment pipeline which applies a series of segment filters to segments.
/// </summary>
/// <remarks>
/// This class allows adding filters to process segments and retrieve the complete segments based on applied filters.
/// </remarks>
public sealed class SegmentFilterFilterPipeline : ISegmentFilterPipeline
{
    private readonly ICollection<ISegmentFilter> filters = new List<ISegmentFilter>();

    /// <summary>
    /// Retrieves the complete segments after applying all filters.
    /// </summary>
    /// <param name="segments">The segments to process.</param>
    /// <returns>The complete segments after applying all filters.</returns>
    public IEnumerable<Segment> Filter(IEnumerable<Segment> segments)
    {
        var filteredSegments = segments;

        foreach (var filter in filters)
        {
            filteredSegments = filter.Filter(filteredSegments);
        }

        return filteredSegments;
    }

    /// <summary>
    /// Adds a segment filter to the pipeline.
    /// </summary>
    /// <param name="filter">The segment filter to add.</param>
    public void AddFilter(ISegmentFilter filter)
    {
        this.filters.Add(filter);
    }

    /// <summary>
    /// Adds a filter of type <typeparamref name="TFilter"/> to the pipeline.
    /// </summary>
    /// <typeparam name="TFilter">The type of filter to add.</typeparam>
    public void AddFilter<TFilter>() where TFilter : ISegmentFilter, new()
    {
        this.filters.Add(new TFilter());
    }
}