using System.Text.Json;
using System.Text.Json.Serialization;
using FFMpegCore;
using NumSharp;
using NumSharp.Utilities;
using Transcriptor.Py.Wrapper.Abstraction;
using Transcriptor.Py.Wrapper.Models;
using WebSocketSharp;
using JsonSerializer = System.Text.Json.JsonSerializer;
using WebSocket = WebSocketSharp.WebSocket;

namespace Transcriptor.Py.Wrapper.Implementation;

public class WhisperTranscriptor : ITranscriptor
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private readonly Uri _serviceUri;
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly Dictionary<Guid, CancellationTokenSource?> pendingCancellationTokenSources = new();

    private WebSocket? _socket;
    private bool _disposed;
    private DateTimeOffset _lastResponseReceived = DateTimeOffset.UtcNow;

    public WhisperTranscriptor(Uri serviceUri)
    {
        this._serviceUri = serviceUri;
    }

    public async Task<TranscriptionSession> TranscribeAsync(
        Uri uri,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default)
    {
        var analysis = await FFProbe.AnalyseAsync(uri, cancellationToken: cancellationToken);
        var numberOfChannel = options.ForcedAudioChannels ?? analysis.PrimaryAudioStream?.Channels ?? 1;

        var sessionId = Guid.NewGuid();

        await InternalTranscribeAsync(
            ct => PcmReader.FromHlsAsync(uri, numberOfChannel, ct),
            sessionId,
            numberOfChannel,
            options,
            cancellationToken);

        return new TranscriptionSession(sessionId, default);
    }

    public async Task<TranscriptionSession> TranscribeAsync(
        string filePath,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default)
    {
        var analysis = await FFProbe.AnalyseAsync(filePath, new FFOptions(), cancellationToken);
        var numberOfChannel = options.ForcedAudioChannels ?? analysis.PrimaryAudioStream?.Channels ?? 1;

        var buffer = PcmReader.FromFile(filePath, numberOfChannel);

        var session = await TranscribeAsync(
            buffer,
            options,
            cancellationToken);

        return session;
    }

    public Task<TranscriptionSession> TranscribeAsync(
        Stream stream,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default)
    {
        return this.TranscribeAsync(stream, options, default, cancellationToken);
    }

    public async Task<TranscriptionSession> TranscribeAsync(
        Stream stream,
        WhisperTranscriptorOptions options,
        IMediaAnalysis? mediaAnalysis = default,
        CancellationToken cancellationToken = default)
    {
        var numberOfChannels = options.ForcedAudioChannels ?? mediaAnalysis?.PrimaryAudioStream?.Channels ?? 1;

        var sessionId = Guid.NewGuid();

        await InternalTranscribeAsync(
            IterateStreamAsync,
            sessionId,
            numberOfChannels,
            options,
            cancellationToken);

        return new TranscriptionSession(sessionId, stream);

        async IAsyncEnumerable<byte[]> IterateStreamAsync(CancellationToken ct)
        {
            const int chunkSize = 4096;

            var buffer = new byte[chunkSize * options.NumberOfSpeaker * 2]; // 2 bytes per sample
            while (!ct.IsCancellationRequested)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct);

                if (bytesRead == 0)
                {
                    yield break;
                }

                yield return buffer;
            }
        }
    }

    private async Task InternalTranscribeAsync(
        Func<CancellationToken, IAsyncEnumerable<byte[]>> streamingTask,
        Guid sessionId,
        int audioChannelCount,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken)
    {
        await this.semaphore.WaitAsync(cancellationToken);

        var serverReady = false;

        this._socket = new WebSocket(this._serviceUri.AbsoluteUri);
        this._socket.OnMessage += (sender, args) =>
        {
            this._lastResponseReceived = DateTimeOffset.UtcNow;

            var payload = JsonSerializer.Deserialize<JsonDocument>(args.Data, _serializerOptions)!;

            if (payload.RootElement.TryGetProperty("message", out var v))
            {
                var rawText = v.GetString();

                if (rawText == "SERVER_READY")
                {
                    serverReady = true;
                    this.TranscriptorReady?.Invoke(sessionId, this);
                }
                else if (rawText == "DISCONNECT")
                {
                    this.SessionEnded?.Invoke(sessionId, TranscriptionSessionEndedReason.ServerDisconnected);
                }
            }
            else if (payload.RootElement.TryGetProperty("segments", out var segmentsJsonElement))
            {
                var hasSpeaker = payload.RootElement.TryGetProperty("speaker", out var speakerElement);
                var segments = segmentsJsonElement.Deserialize<Segment[]>(_serializerOptions)!;
                var speaker = hasSpeaker ? speakerElement.GetString() : default;

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
        this._socket.Connect();

        CancellationTokenSource? cts = default;
        CancellationToken ct = cancellationToken;

        if (cancellationToken == CancellationToken.None)
        {
            cts = new CancellationTokenSource();
            ct = cts.Token;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                this._socket.Send(JsonSerializer.Serialize(new
                {
                    uid = sessionId,
                    language = options.Language,
                    task = "transcribe",
                    model = options.Model,
                    use_vad = options.UseVoiceActivityDetection,
                    type = options.ModelType.ToString(),
                    channel = audioChannelCount,
                    multilingual = options.IsMultiLanguage,
                }));

                while (!serverReady)
                {
                    await Task.Delay(100, ct);
                }

                await foreach (var buffer in streamingTask(ct))
                {
                    if (!this._socket.IsAlive)
                    {
                        this.SessionEnded?.Invoke(sessionId, TranscriptionSessionEndedReason.ServerDisconnected);
                        break;
                    }

                    await Task.Delay(options.TranscriptionDelay, ct);

                    var data = BytesToFloatArray(buffer).ToByteArray();
                    this._socket.Send(data);
                }

                while (!ct.IsCancellationRequested &&
                       DateTimeOffset.UtcNow.Subtract(this._lastResponseReceived).TotalSeconds <
                       options.TranscriptionTimeout.Seconds)
                {
                    await Task.Delay(1000, ct);
                }

                this._socket.Send("END_OF_AUDIO"u8.ToArray());

                this._socket.Close(CloseStatusCode.Normal);

                this.SessionEnded?.Invoke(sessionId, TranscriptionSessionEndedReason.Completed);
            }
            catch (TaskCanceledException)
            {
                this._socket.Close(CloseStatusCode.Normal);

                if (this.pendingCancellationTokenSources.Remove(sessionId, out var t))
                {
                    t?.Dispose();
                }

                this.SessionEnded?.Invoke(sessionId, TranscriptionSessionEndedReason.Error);
            }
        }, ct);

        this.pendingCancellationTokenSources.Add(sessionId, cts);

        this.semaphore.Release();
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

    private static NDArray BytesToFloatArray(byte[] audioBytes)
    {
        var rawData = CreateInt16Buffer(audioBytes);
        return rawData.astype(np.float32) / 32768.0;

        static NDArray CreateInt16Buffer(byte[] bytes)
        {
            var length = bytes.Length / InfoOf<short>.Size;
            var values = new int[length];
            for (var index = 0; index < length; ++index)
                values[index] = BitConverter.ToInt16(bytes, index * InfoOf<short>.Size);
            return new NDArray(values);
        }
    }

    public event TranscriptorReadyEventHandler? TranscriptorReady;
    public event TranscriptionMessageArrivedHandler? MessageArrived;
    public event TranscriptionSessionEndedHandler? SessionEnded;

    internal void Dispose(bool disposing)
    {
        if (this._disposed || !disposing)
        {
            return;
        }

        foreach (var session in this.pendingCancellationTokenSources)
        {
            session.Value?.Cancel();
        }

        this._disposed = true;
    }

    public void Dispose()
    {
        this.Dispose(true);

        GC.SuppressFinalize(this);
    }

    public IDisposable Subscribe(IObserver<TranscriptResult> observer)
    {
        this.MessageArrived += (id, speaker, segments) =>
        {
            observer.OnNext(new TranscriptResult(
                id,
                speaker,
                segments.Select(s => new TranscriptMessage(s.Start, s.End, s.Text))));

            return Task.CompletedTask;
        };

        this.SessionEnded += (_, _) =>
        {
            observer.OnCompleted();

            return Task.CompletedTask;
        };

        return System.Reactive.Disposables.Disposable.Empty;
    }
}