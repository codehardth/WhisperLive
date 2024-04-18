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
    Task<TranscriptionSession> StartAsync(
        Uri uri,
        TranscriptorConfiguration configuration,
        CancellationToken cancellationToken = default);

    Task<TranscriptionSession> StartAsync(
        string filePath,
        TranscriptorConfiguration configuration,
        CancellationToken cancellationToken = default);

    Task<TranscriptionSession> TranscribeAsync(
        Stream stream,
        TranscriptorConfiguration configuration,
        CancellationToken cancellationToken = default);

    Task<TranscriptionSession> TranscribeAsync(
        Stream stream,
        TranscriptorConfiguration configuration,
        int audioChannelCount = 1,
        CancellationToken cancellationToken = default);

    Task StopAsync(Guid sessionId, CancellationToken cancellationToken = default);

    event TranscriptorReadyEventHandler TranscriptorReady;

    event TranscriptionMessageArrivedHandler MessageArrived;

    event TranscriptionSessionEndedHandler SessionEnded;
}