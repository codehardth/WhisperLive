using Transcriptor.Py.Wrapper.Abstraction;
using Transcriptor.Py.Wrapper.Models;

namespace Transcriptor.Py.Wrapper.Implementation;

/// <summary>
/// Represents a segment filter that retains only segments with the same start time, keeping the last segment with that start time.
/// </summary>
public sealed class LastSegmentPerStartTimeFilter : ISegmentFilter
{
    private readonly SortedList<double, Segment> sortedMessages = new();
    private double lastSegmentTime = -1;

    /// <summary>
    /// Retrieves the complete segments after applying the filter.
    /// </summary>
    /// <param name="segments">The segments to filter.</param>
    /// <returns>The complete segments after applying the filter.</returns>
    public IEnumerable<Segment> Filter(IEnumerable<Segment> segments)
    {
        var distinctMessages =
            segments
                .GroupBy(m => m.Start)
                .Select(g => g.Last());

        foreach (var segment in distinctMessages)
        {
            if (!this.sortedMessages.Any())
            {
                this.sortedMessages.Add(segment.Start, segment);
            }
            else
            {
                var messageIndex = this.sortedMessages.IndexOfKey(segment.Start);

                if (messageIndex < 0)
                {
                    this.sortedMessages.Add(segment.Start, segment);
                }
                else
                {
                    var current = this.sortedMessages[segment.Start];

                    if (current.End < segment.End)
                    {
                        this.sortedMessages[segment.Start] = segment;
                    }
                }
            }
        }

        var last = sortedMessages.Values.LastOrDefault();

        if (last is null || last.Start.Equals(lastSegmentTime))
        {
            return Enumerable.Empty<Segment>();
        }

        lastSegmentTime = last.Start;

        return sortedMessages.Values;
    }
}