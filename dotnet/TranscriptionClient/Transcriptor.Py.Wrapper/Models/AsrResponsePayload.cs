namespace Transcriptor.Py.Wrapper.Models;

public sealed record Segment(double Start, double End, string Text);

public sealed record AsrResponsePayload(Guid Uid, string? Message, string? Label, Segment[] Segments);