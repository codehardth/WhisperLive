using WhisperLive.Abstraction.Models;

namespace WhisperLive.Abstraction;

/// <summary>
/// Defines the contract for segment filters.
/// </summary>
public interface ISegmentFilter
{
    /// <summary>
    /// Retrieves the complete segments after applying the filter.
    /// </summary>
    /// <param name="segments">The segments to filter.</param>
    /// <returns>The complete segments after applying the filter.</returns>
    IEnumerable<Segment> Filter(IEnumerable<Segment> segments);
}
