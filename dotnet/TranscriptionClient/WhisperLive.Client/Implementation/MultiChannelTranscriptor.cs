using FFMpegCore;
using NumSharp;
using WebSocketSharp;
using WhisperLive.Abstraction;
using WhisperLive.Abstraction.Configurations;
using WhisperLive.Abstraction.Models;
using WhisperLive.Client.Extensions;
using WhisperLive.Client.Helpers;

namespace WhisperLive.Client.Implementation;

public class MultiChannelTranscriptor(ITranscriptionServerCoordinator coordinator) : WhisperTranscriptor
{
    private readonly ICollection<WebSocket> sockets = new List<WebSocket>();

    private int AudioChannelCount { get; set; }

    public override async Task<TranscriptionSession> TranscribeAsync(
        Uri uri,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default)
    {
        var analysis = await FFProbe.AnalyseAsync(uri, cancellationToken: cancellationToken);
        var numberOfChannel = analysis.PrimaryAudioStream?.Channels ?? 1;

        var sessionId = Guid.NewGuid();

        await InternalTranscribeAsync(
            PcmReader.FromHlsAsync(uri, numberOfChannel, cancellationToken),
            sessionId,
            numberOfChannel,
            options,
            cancellationToken);

        return new TranscriptionSession(sessionId, default);
    }

    public override async Task<TranscriptionSession> TranscribeAsync(
        string filePath,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default)
    {
        var analysis = await FFProbe.AnalyseAsync(filePath, new FFOptions(), cancellationToken);
        var numberOfChannel = analysis.PrimaryAudioStream?.Channels ?? 1;

        var buffer = PcmReader.FromFile(filePath, numberOfChannel);

        var session = await TranscribeAsync(
            buffer,
            options,
            numberOfChannel,
            cancellationToken);

        return session;
    }

    protected override Task SendPacketToServerAsync(WebSocket webSocket, byte[] buffer)
    {
        if (this.AudioChannelCount == 1)
        {
            return base.SendPacketToServerAsync(webSocket, buffer);
        }

        var decode = Decode(Numpy.bytes_to_float_array(buffer).ToByteArray(), this.AudioChannelCount);

        for (var channel = 0; channel < this.AudioChannelCount; channel++)
        {
            var specificChannelBuffer = decode[Slice.All, channel];

            var socket = this.sockets.ElementAt(channel);

            socket.Send(specificChannelBuffer.ToByteArray());
        }

        return Task.CompletedTask;
    }

    protected override async Task ProcessStreamAsync(
        IAsyncEnumerable<byte[]> stream,
        Guid sessionId,
        int audioChannelCount,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken)
    {
        this.AudioChannelCount = audioChannelCount;

        const int defaultPort = 19090;

        var connectionsTask = new Task<WebSocket>[audioChannelCount];

        for (var channel = 1; channel <= audioChannelCount; channel++)
        {
            var port = defaultPort + channel - 1;
            var endpoint = await coordinator.GetTranscriptionServerEndpointAsync(port, cancellationToken);

            var speakerLabel = $"Channel {channel}";

            var task = Task.Run(async () =>
            {
                var socket = base.CreateWebSocket(endpoint, sessionId, options, speakerLabel);
                await socket.InitiateConnectionAsync(sessionId, options);

                return socket;
            }, cancellationToken);

            connectionsTask[channel - 1] = task;
        }

        var connections = await Task.WhenAll(connectionsTask);

        foreach (var connection in connections)
            this.sockets.Add(connection);

        // Start just 1 runner since stream can't be read by multiple readers.
        var firstSocket = this.sockets.First();
        _ = base.RunStreamingLoopAsync(firstSocket, stream, sessionId, options, cancellationToken);
    }

    private static NDArray Decode(byte[] buffer, int channels)
    {
        var array = Numpy.frombuffer(buffer, np.float32);
        var chunkLength = array.size / channels;
        var result = np.reshape(array, (chunkLength, channels));

        return result;
    }
}