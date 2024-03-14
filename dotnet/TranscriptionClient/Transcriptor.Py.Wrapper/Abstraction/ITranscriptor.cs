using Transcriptor.Py.Wrapper.Implementation;
using Transcriptor.Py.Wrapper.Models;

namespace Transcriptor.Py.Wrapper.Abstraction;

public delegate void TranscriptorReadyEventHandler(Guid sessionId, ITranscriptor sender);

public delegate Task TranscriptionMessageArrivedHandler(Guid sessionId, string? speaker, Segment[] segments);

public delegate Task TranscriptionSessionEndedHandler(Guid sessionId);

public interface ITranscriptor : IObservable<TranscriptResult>, IDisposable
{
    Task<Guid> TranscribeAsync(
        Uri uri,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default);

    Task StopAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<Guid> TranscribeAsync(
        string filePath,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default);

    event TranscriptorReadyEventHandler TranscriptorReady;

    event TranscriptionMessageArrivedHandler MessageArrived;

    event TranscriptionSessionEndedHandler SessionEnded;
}