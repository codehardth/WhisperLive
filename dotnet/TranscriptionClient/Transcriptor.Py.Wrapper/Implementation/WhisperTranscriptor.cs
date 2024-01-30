using System.Buffers;
using System.Net.WebSockets;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Transcriptor.Py.Wrapper.Abstraction;
using Transcriptor.Py.Wrapper.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Transcriptor.Py.Wrapper.Implementation;

public class WhisperTranscriptor : ITranscriptor, IObservable<TranscriptMessage>
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private readonly Uri _serviceUri;
    private ClientWebSocket? _socket;
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

    public async Task StartAsync(int index, CancellationToken cancellationToken = default)
    {
        await this.StopAsync(cancellationToken);

        this._socket = new ClientWebSocket();
        this._socket.Options.SetRequestHeader("x-device-index", index.ToString());

        await this._socket.ConnectAsync(this._serviceUri, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (this._socket is not null && this._socket.State is not WebSocketState.Closed)
        {
            await this._socket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, cancellationToken);
        }
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

    public IDisposable Subscribe(IObserver<TranscriptMessage> observer)
    {
        var sharedMemoryPool = ArrayPool<byte>.Shared;
        var buffer = sharedMemoryPool.Rent(4096);

        var firstMessageArrived = false;

        try
        {
            while (this._socket?.State == WebSocketState.Open)
            {
                Array.Clear(buffer);

                this._socket.ReceiveAsync(buffer, CancellationToken.None).Wait();

                var json = Encoding.UTF8.GetString(buffer).TrimEnd((char)0);

                if (json == string.Empty)
                {
                    continue;
                }

                if (!firstMessageArrived)
                {
                    this.TranscriptorReady?.Invoke(this);

                    firstMessageArrived = true;
                }

                var obj = JsonSerializer.Deserialize<TranscriptResult>(json, _serializerOptions)!;

                foreach (var message in obj.Messages)
                {
                    observer.OnNext(message);
                }
            }
        }
        finally
        {
            sharedMemoryPool.Return(buffer);
        }

        observer.OnCompleted();

        return System.Reactive.Disposables.Disposable.Empty;
    }
}