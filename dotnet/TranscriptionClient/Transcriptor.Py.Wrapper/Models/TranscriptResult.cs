namespace Transcriptor.Py.Wrapper.Models;

public sealed record TranscriptMessage(double Start, double End, string Text);

public sealed record TranscriptResult(Guid SessionId, string? Speaker, IEnumerable<TranscriptMessage> Messages);