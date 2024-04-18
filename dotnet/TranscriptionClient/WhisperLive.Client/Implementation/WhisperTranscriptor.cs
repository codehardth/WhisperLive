using System.Runtime.CompilerServices;
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

    public abstract Task<TranscriptionSession> StartAsync(
        Uri uri,
        TranscriptorConfiguration configuration,
        CancellationToken cancellationToken = default);

    public abstract Task<TranscriptionSession> StartAsync(
        string filePath,
        TranscriptorConfiguration configuration,
        CancellationToken cancellationToken = default);

    public Task<TranscriptionSession> TranscribeAsync(
        Stream stream,
        TranscriptorConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        return this.TranscribeAsync(stream, configuration, 1, cancellationToken);
    }

    public async Task<TranscriptionSession> TranscribeAsync(
        Stream stream,
        TranscriptorConfiguration configuration,
        int audioChannelCount = 1,
        CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid();

        await InternalTranscribeAsync(
            IterateStreamAsync,
            sessionId,
            audioChannelCount,
            configuration,
            cancellationToken);

        return new TranscriptionSession(sessionId, stream);

        async IAsyncEnumerable<byte[]> IterateStreamAsync([EnumeratorCancellation] CancellationToken ct)
        {
            const int chunkSize = 4096;

            var buffer = new byte[chunkSize * audioChannelCount * 2]; // 2 bytes per sample
            while (!ct.IsCancellationRequested)
            {
                Array.Clear(buffer);
                var bytesRead = await stream.ReadAsync(buffer, ct);

                if (bytesRead == 0)
                {
                    yield break;
                }

                yield return buffer;
            }
        }
    }

    protected async Task InternalTranscribeAsync(
        Func<CancellationToken, IAsyncEnumerable<byte[]>> streamFunc,
        Guid sessionId,
        int audioChannelCount,
        TranscriptorConfiguration configuration,
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

        await ProcessStreamAsync(streamFunc(ct), sessionId, audioChannelCount, configuration, ct);

        this.pendingCancellationTokenSources.Add(sessionId, cts);
        this.semaphore.Release();
    }

    protected abstract Task ProcessStreamAsync(
        IAsyncEnumerable<byte[]> stream,
        Guid sessionId,
        int audioChannelCount,
        TranscriptorConfiguration configuration,
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
        TranscriptorConfiguration configuration,
        CancellationToken ct)
    {
        var socket = CreateWebSocket(endpoint, sessionId, configuration);
        await socket.InitiateConnectionAsync(sessionId, configuration);

        _ = Task.Run(
            () => RunStreamingLoopAsync(socket, stream, sessionId, configuration, ct),
            ct);
    }

    public virtual async Task StopAsync(Guid sessionId, CancellationToken cancellationToken = default)
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
        TranscriptorConfiguration configuration,
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

                if (configuration.SegmentFilter is { } filter)
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
        TranscriptorConfiguration configuration,
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

                await Task.Delay(configuration.TranscriptionDelay, ct);
                await this.SendPacketToServerAsync(socket, buffer);
            }

            while (!ct.IsCancellationRequested &&
                   DateTimeOffset.UtcNow.Subtract(lastResponseReceived).TotalSeconds <
                   configuration.TranscriptionTimeout.Seconds)
            {
                await Task.Delay(100, ct);
            }

            socket.CloseConnection();
            this.SessionEnded?.Invoke(sessionId, TranscriptionSessionEndedReason.Completed);
        }
        catch (Exception ex)
        {
            socket.CloseConnection();
            var status =
                ex switch
                {
                    OperationCanceledException or TaskCanceledException
                        => TranscriptionSessionEndedReason.Completed,
                    _ => TranscriptionSessionEndedReason.Error,
                };

            this.SessionEnded?.Invoke(sessionId, status);
        }
        finally
        {
            await this.StopAsync(sessionId, ct);
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