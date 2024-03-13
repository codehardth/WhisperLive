using System.Buffers;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Transcriptor.Py.Wrapper.Abstraction;
using Transcriptor.Py.Wrapper.Models;
using WebSocketSharp.Net;
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

    private WebSocket? _socket;
    private bool _disposed;

    public WhisperTranscriptor(Uri serviceUri)
    {
        this._serviceUri = serviceUri;
        PortAudioSharp.PortAudio.LoadNativeLibrary();
        PortAudioSharp.PortAudio.Initialize();
    }

    public async IAsyncEnumerable<InputInterface> GetInputInterfacesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var count = PortAudioSharp.PortAudio.DeviceCount;

        for (var index = 0; index < count; index++)
        {
            var device = PortAudioSharp.PortAudio.GetDeviceInfo(index);

            if (device.maxInputChannels <= 0)
            {
                continue;
            }

            await Task.Delay(1, cancellationToken);

            yield return new InputInterface(index, device.name);
        }
    }

    public async Task StartRecordAsync(
        int index,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default)
    {
        await this.StopAsync(cancellationToken);

        this._socket = new WebSocket(this._serviceUri.AbsoluteUri);
        this._socket.SetCookie(new Cookie("x-device-index", index.ToString()));
        this._socket.SetCookie(new Cookie("x-model-type", options.ModelType.ToString()));
        this._socket.SetCookie(new Cookie("x-model-size", options.ModelSize));
        this._socket.SetCookie(new Cookie("x-num-speaker", options.NumberOfSpeaker.ToString()));

        if (!string.IsNullOrWhiteSpace(options.Language))
        {
            this._socket.SetCookie(new Cookie("x-language", options.Language));
        }

        this._socket.SetCookie(new Cookie("x-is-multilang", options.IsMultiLanguage.ToString()));

        this._socket.Connect();
    }

    public async Task StartRecordAsync(
        Uri uri,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default)
    {
        await this.StopAsync(cancellationToken);

        this._socket = new WebSocket(this._serviceUri.AbsoluteUri);
        this._socket.SetCookie(new Cookie("x-hls-url", uri.ToString()));
        this._socket.SetCookie(new Cookie("x-model-type", options.ModelType.ToString()));
        this._socket.SetCookie(new Cookie("x-model-size", options.ModelSize));
        this._socket.SetCookie(new Cookie("x-num-speaker", options.NumberOfSpeaker.ToString()));

        if (!string.IsNullOrWhiteSpace(options.Language))
        {
            this._socket.SetCookie(new Cookie("x-language", options.Language));
        }

        this._socket.SetCookie(new Cookie("x-is-multilang", options.IsMultiLanguage.ToString()));

        this._socket.Connect();
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (this._socket is not null && this._socket.IsAlive)
        {
            this._socket.Close();
        }

        return Task.CompletedTask;
    }

    public async Task TranscriptAsync(
        string filePath,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(filePath);
        }

        await this.StopAsync(cancellationToken);

        this._socket = new WebSocket(this._serviceUri.AbsoluteUri);
        this._socket.SetCookie(new Cookie("x-file-path", filePath));
        this._socket.SetCookie(new Cookie("x-model-type", options.ModelType.ToString()));
        this._socket.SetCookie(new Cookie("x-model-size", options.ModelSize));

        if (!string.IsNullOrWhiteSpace(options.Language))
        {
            this._socket.SetCookie(new Cookie("x-language", options.Language));
        }

        this._socket.SetCookie(new Cookie("x-is-multilang", options.IsMultiLanguage.ToString()));

        this._socket.Connect();
    }

    public event TranscriptorReadyEventHandler? TranscriptorReady;

    internal void Dispose(bool disposing)
    {
        if (this._disposed || !disposing)
        {
            return;
        }

        this.StopAsync().Wait();

        this._disposed = true;
    }

    public void Dispose()
    {
        this.Dispose(true);

        GC.SuppressFinalize(this);
    }

    public IDisposable Subscribe(IObserver<TranscriptResult> observer)
    {
        if (this._socket is null)
        {
            observer.OnCompleted();

            return System.Reactive.Disposables.Disposable.Empty;
        }

        var sharedMemoryPool = ArrayPool<byte>.Shared;
        var buffer = sharedMemoryPool.Rent(4096);

        this._socket.OnError += (_, args) =>
        {
            sharedMemoryPool.Return(buffer);
            observer.OnError(args.Exception);
        };
        this._socket.OnMessage += (sender, args) =>
        {
            Array.Clear(buffer);

            var json = args.Data;

            if (json != string.Empty)
            {
                var result = JsonSerializer.Deserialize<TranscriptResult>(json, _serializerOptions)!;

                observer.OnNext(result);
            }
        };
        this._socket.OnClose += (sender, args) =>
        {
            sharedMemoryPool.Return(buffer);

            observer.OnCompleted();
        };

        return System.Reactive.Disposables.Disposable.Empty;
    }
}