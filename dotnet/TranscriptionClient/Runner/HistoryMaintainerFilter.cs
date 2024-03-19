using Transcriptor.Py.Wrapper.Abstraction;
using Transcriptor.Py.Wrapper.Models;

namespace Runner;

public class HistoryMaintainerFilter : ISegmentFilter
{
    public readonly IReadOnlyCollection<Segment> History = new List<Segment>();

    public IEnumerable<Segment> Filter(IEnumerable<Segment> segments)
    {
        if (this.History is List<Segment> list)
        {
            list.AddRange(segments);
        }

        return segments;
    }
}