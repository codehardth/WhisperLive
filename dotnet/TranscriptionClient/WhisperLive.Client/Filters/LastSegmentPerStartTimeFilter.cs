using WhisperLive.Abstraction;
using WhisperLive.Abstraction.Models;

namespace WhisperLive.Client.Filters;

/// <summary>
/// Represents a segment filter that retains only segments with the same start time, keeping the last segment with that start time.
/// </summary>
public sealed class LastSegmentPerStartTimeFilter : ISegmentFilter
{
    private readonly int bufferThresholdSecond;
    private readonly SortedList<double, Segment> sortedMessages = new();
    private double lastSegmentTime = -1;
    private DateTimeOffset lastSent = DateTimeOffset.MinValue;

    public LastSegmentPerStartTimeFilter(int bufferThresholdSecond)
    {
        this.bufferThresholdSecond = bufferThresholdSecond;
    }

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
        var lastSentOverThreshold =
            DateTimeOffset.UtcNow.Subtract(this.lastSent).TotalSeconds > this.bufferThresholdSecond;

        if (last is null || last.Start.Equals(lastSegmentTime) || !lastSentOverThreshold)
        {
            return Enumerable.Empty<Segment>();
        }

        lastSegmentTime = last.Start;
        this.lastSent = DateTimeOffset.UtcNow;

        return sortedMessages.Values;
    }
}