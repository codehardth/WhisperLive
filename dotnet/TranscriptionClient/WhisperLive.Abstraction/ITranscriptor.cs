using WhisperLive.Abstraction.Configurations;
using WhisperLive.Abstraction.Models;

namespace WhisperLive.Abstraction;

public delegate void TranscriptorReadyEventHandler(Guid sessionId, ITranscriptor sender);

public delegate Task TranscriptionMessageArrivedHandler(Guid sessionId, string? speaker, IEnumerable<Segment> segments);

public enum TranscriptionSessionEndedReason
{
    Completed,
    ServerDisconnected,
    Error,
}

public delegate Task TranscriptionSessionEndedHandler(Guid sessionId, TranscriptionSessionEndedReason reason);

public interface ITranscriptor : IDisposable
{
    Task<TranscriptionSession> TranscribeAsync(
        Uri uri,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default);

    Task<TranscriptionSession> TranscribeAsync(
        string filePath,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default);

    Task<TranscriptionSession> TranscribeAsync(
        Stream stream,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default);

    Task<TranscriptionSession> TranscribeAsync(
        Stream stream,
        WhisperTranscriptorOptions options,
        int audioChannelCount = 1,
        CancellationToken cancellationToken = default);

    Task StopAsync(Guid sessionId, CancellationToken cancellationToken = default);

    event TranscriptorReadyEventHandler TranscriptorReady;

    event TranscriptionMessageArrivedHandler MessageArrived;

    event TranscriptionSessionEndedHandler SessionEnded;
}