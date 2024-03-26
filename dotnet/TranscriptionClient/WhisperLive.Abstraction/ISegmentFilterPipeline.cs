namespace WhisperLive.Abstraction;

/// <summary>
/// Defines the contract for segment pipelines, which are capable of applying filters to segments.
/// </summary>
public interface ISegmentFilterPipeline : ISegmentFilter
{
    /// <summary>
    /// Adds a segment filter to the pipeline.
    /// </summary>
    /// <param name="filter">The segment filter to add.</param>
    void AddFilter(ISegmentFilter filter);

    /// <summary>
    /// Adds a filter of type <typeparamref name="TFilter"/> to the pipeline.
    /// </summary>
    /// <typeparam name="TFilter">The type of filter to add.</typeparam>
    void AddFilter<TFilter>() where TFilter : ISegmentFilter, new();
}