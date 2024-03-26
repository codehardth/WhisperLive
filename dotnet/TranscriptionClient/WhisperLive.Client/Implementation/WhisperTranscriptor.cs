using System.Text.Json;
using System.Text.Json.Serialization;
using WebSocketSharp;
using WhisperLive.Abstraction;
using WhisperLive.Abstraction.Configurations;
using WhisperLive.Abstraction.Models;
using WhisperLive.Client.Extensions;
using WhisperLive.Client.Helpers;
using JsonSerializer = System.Text.Json.JsonSerializer;
using WebSocket = WebSocketSharp.WebSocket;

namespace WhisperLive.Client.Implementation;

public abstract class WhisperTranscriptor : ITranscriptor
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly Dictionary<Guid, CancellationTokenSource?> pendingCancellationTokenSources = new();

    private bool _disposed;

    protected WhisperTranscriptor()
    {
    }

    public abstract Task<TranscriptionSession> TranscribeAsync(
        Uri uri,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default);

    public abstract Task<TranscriptionSession> TranscribeAsync(
        string filePath,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default);

    public Task<TranscriptionSession> TranscribeAsync(
        Stream stream,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default)
    {
        return this.TranscribeAsync(stream, options, 1, cancellationToken);
    }

    public async Task<TranscriptionSession> TranscribeAsync(
        Stream stream,
        WhisperTranscriptorOptions options,
        int audioChannelCount = 1,
        CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid();

        await InternalTranscribeAsync(
            IterateStreamAsync(),
            sessionId,
            audioChannelCount,
            options,
            cancellationToken);

        return new TranscriptionSession(sessionId, stream);

        async IAsyncEnumerable<byte[]> IterateStreamAsync()
        {
            const int chunkSize = 4096;

            var buffer = new byte[chunkSize * audioChannelCount * 2]; // 2 bytes per sample
            while (!cancellationToken.IsCancellationRequested)
            {
                Array.Clear(buffer);
                var bytesRead = await stream.ReadAsync(buffer, cancellationToken);

                if (bytesRead == 0)
                {
                    yield break;
                }

                yield return buffer;
            }
        }
    }

    protected async Task InternalTranscribeAsync(
        IAsyncEnumerable<byte[]> stream,
        Guid sessionId,
        int audioChannelCount,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken)
    {
        await this.semaphore.WaitAsync(cancellationToken);

        CancellationTokenSource? cts = default;
        CancellationToken ct = cancellationToken;

        if (cancellationToken == CancellationToken.None)
        {
            cts = new CancellationTokenSource();
            ct = cts.Token;
        }

        await ProcessStreamAsync(stream, sessionId, audioChannelCount, options, cancellationToken);

        this.pendingCancellationTokenSources.Add(sessionId, cts);
        this.semaphore.Release();
    }

    protected abstract Task ProcessStreamAsync(
        IAsyncEnumerable<byte[]> stream,
        Guid sessionId,
        int audioChannelCount,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken);

    protected virtual Task SendPacketToServerAsync(
        WebSocket socket,
        byte[] buffer)
    {
        var encodedBuffer = Numpy.bytes_to_float_array(buffer);
        var packets = encodedBuffer.ToByteArray();
        socket.Send(packets);

        return Task.CompletedTask;
    }

    protected async Task OpenWebSocketConnectionAndStreamAudioAsync(
        Uri endpoint,
        Guid sessionId,
        IAsyncEnumerable<byte[]> stream,
        WhisperTranscriptorOptions options,
        CancellationToken ct)
    {
        var socket = CreateWebSocket(endpoint, sessionId, options);
        await socket.InitiateConnectionAsync(sessionId, options);

        _ = Task.Run(
            () => RunStreamingLoopAsync(socket, stream, sessionId, options, ct),
            ct);
    }

    public async Task StopAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await this.semaphore.WaitAsync(CancellationToken.None);

        if (this.pendingCancellationTokenSources.Remove(sessionId, out var cts))
        {
            await (cts?.CancelAsync() ?? Task.CompletedTask);
        }

        this.semaphore.Release();
    }

    protected WebSocket CreateWebSocket(
        Uri endpoint,
        Guid sessionId,
        WhisperTranscriptorOptions options,
        string label = "")
    {
        var socket = new WebSocket(endpoint.AbsoluteUri);
        socket.OnMessage += (sender, args) =>
        {
            var payload = JsonSerializer.Deserialize<JsonDocument>(args.Data, _serializerOptions)!;

            if (payload.RootElement.TryGetProperty("message", out var v))
            {
                var rawText = v.GetString();

                if (rawText == "SERVER_READY")
                {
                    this.TranscriptorReady?.Invoke(sessionId, this);
                }
                else if (rawText == "DISCONNECT")
                {
                    this.SessionEnded?.Invoke(sessionId, TranscriptionSessionEndedReason.ServerDisconnected);
                }
            }
            else if (payload.RootElement.TryGetProperty("segments", out var segmentsJsonElement))
            {
                var segments = segmentsJsonElement.Deserialize<Segment[]>(_serializerOptions)!;

                if (segments.Length == 0)
                {
                    return;
                }

                var hasSpeaker = payload.RootElement.TryGetProperty("speaker", out var speakerElement);
                var speaker = hasSpeaker ? speakerElement.GetString() : label;

                if (options.SegmentFilter is { } filter)
                {
                    var filteredSegments = filter.Filter(segments);

                    if (filteredSegments.Any())
                    {
                        this.MessageArrived?.Invoke(sessionId, speaker, filteredSegments);
                    }
                }
                else
                {
                    this.MessageArrived?.Invoke(sessionId, speaker, segments);
                }
            }
        };
        socket.Connect();

        return socket;
    }

    protected async Task RunStreamingLoopAsync(
        WebSocket socket,
        IAsyncEnumerable<byte[]> stream,
        Guid sessionId,
        WhisperTranscriptorOptions options,
        CancellationToken ct)
    {
        var lastResponseReceived = DateTimeOffset.UtcNow;
        socket.OnMessage += (sender, message) => lastResponseReceived = DateTimeOffset.UtcNow;

        try
        {
            await foreach (var buffer in stream.WithCancellation(ct))
            {
                if (!socket.IsAlive)
                {
                    this.SessionEnded?.Invoke(sessionId, TranscriptionSessionEndedReason.ServerDisconnected);
                    return;
                }

                await Task.Delay(options.TranscriptionDelay, ct);
                await this.SendPacketToServerAsync(socket, buffer);
            }

            while (!ct.IsCancellationRequested &&
                   DateTimeOffset.UtcNow.Subtract(lastResponseReceived).TotalSeconds <
                   options.TranscriptionTimeout.Seconds)
            {
                await Task.Delay(1000, ct);
            }

            socket.Send("END_OF_AUDIO"u8.ToArray());
            socket.Close(CloseStatusCode.Normal);

            this.SessionEnded?.Invoke(sessionId, TranscriptionSessionEndedReason.Completed);
        }
        catch (TaskCanceledException)
        {
            socket.Close(CloseStatusCode.Normal);
            this.SessionEnded?.Invoke(sessionId, TranscriptionSessionEndedReason.Error);
        }
    }

    public event TranscriptorReadyEventHandler? TranscriptorReady;
    public event TranscriptionMessageArrivedHandler? MessageArrived;
    public event TranscriptionSessionEndedHandler? SessionEnded;

    protected virtual void Dispose(bool disposing)
    {
        if (this._disposed || !disposing)
        {
            return;
        }

        foreach (var session in this.pendingCancellationTokenSources.Select(s => s.Value))
        {
            session?.Cancel();
        }

        this._disposed = true;
    }

    public void Dispose()
    {
        this.Dispose(true);

        GC.SuppressFinalize(this);
    }
}