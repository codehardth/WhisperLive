using WhisperLive.Abstraction;
using WhisperLive.Abstraction.Models;

namespace Runner;

public class RemoveUnwantedWordsFilter : ISegmentFilter
{
    private readonly IReadOnlyCollection<string> unwatedWords = new[]
    {
        "Thank you for watching",
        "โปรดติดตามตอนต่อไป",
        "บริษัท A-TECH",
        "บริษัท B-TECH",
        "บริษัท C-TECH",
        "🎵",
        "!",
    };

    public IEnumerable<Segment> Filter(IEnumerable<Segment> segments)
    {
        return segments.Select(
            segment => segment with
            {
                Text = unwatedWords.Aggregate(segment.Text, (c, w) => c.Replace(w, string.Empty)),
            });
    }
}