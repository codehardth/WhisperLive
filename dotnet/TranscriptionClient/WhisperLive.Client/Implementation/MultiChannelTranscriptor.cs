using System.Buffers;
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

    public override async Task<TranscriptionSession> StartAsync(
        Uri uri,
        TranscriptorConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var analysis = await FFProbe.AnalyseAsync(uri, cancellationToken: cancellationToken);
        var numberOfChannel = analysis.PrimaryAudioStream?.Channels ?? 1;

        var sessionId = Guid.NewGuid();

        await InternalTranscribeAsync(
            ct => PcmReader.FromHlsAsync(uri, numberOfChannel, ct),
            sessionId,
            numberOfChannel,
            configuration,
            cancellationToken);

        return new TranscriptionSession(sessionId, default);
    }

    public override async Task<TranscriptionSession> StartAsync(
        string filePath,
        TranscriptorConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var analysis = await FFProbe.AnalyseAsync(filePath, new FFOptions(), cancellationToken);
        var numberOfChannel = analysis.PrimaryAudioStream?.Channels ?? 1;

        var buffer = PcmReader.FromFile(filePath, numberOfChannel);

        var session = await TranscribeAsync(
            buffer,
            configuration,
            numberOfChannel,
            cancellationToken);

        return session;
    }

    public override async Task StopAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        foreach (var socket in this.sockets)
        {
            socket.CloseConnection();
        }

        await base.StopAsync(sessionId, cancellationToken);
    }

    protected override async Task SendPacketToServerAsync(WebSocket webSocket, byte[] buffer)
    {
        if (this.AudioChannelCount == 1)
        {
            await base.SendPacketToServerAsync(webSocket, buffer);

            return;
        }

        var deinterleaveBuffers = DeinterleaveChannels(buffer, this.AudioChannelCount, 2);

        for (var channel = 0; channel < this.AudioChannelCount; channel++)
        {
            var socket = this.sockets.ElementAt(channel);
            var bytes = deinterleaveBuffers.ElementAt(channel);

            await base.SendPacketToServerAsync(socket, bytes);
        }

        static IEnumerable<byte[]> DeinterleaveChannels(byte[] interleavedBuffer, int channelCount, int bytesPerSample)
        {
            var samplesPerChannel = interleavedBuffer.Length / (channelCount * bytesPerSample);
            var bufferSize = samplesPerChannel * bytesPerSample;

            var buffer = new byte[bufferSize];
            for (var i = 0; i < channelCount; i++)
            {
                for (var j = 0; j < samplesPerChannel; j++)
                {
                    var srcOffset = (j * channelCount + i) * bytesPerSample;
                    var destOffset = j * bytesPerSample;
                    Buffer.BlockCopy(interleavedBuffer, srcOffset, buffer, destOffset, bytesPerSample);
                }

                yield return buffer;
            }
        }
    }

    protected override async Task ProcessStreamAsync(
        IAsyncEnumerable<byte[]> stream,
        Guid sessionId,
        int audioChannelCount,
        TranscriptorConfiguration configuration,
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
                var socket = base.CreateWebSocket(endpoint, sessionId, configuration, speakerLabel);
                await socket.InitiateConnectionAsync(sessionId, configuration);

                return socket;
            }, cancellationToken);

            connectionsTask[channel - 1] = task;
        }

        var connections = await Task.WhenAll(connectionsTask);

        foreach (var connection in connections)
            this.sockets.Add(connection);

        // Start just 1 runner since stream can't be read by multiple readers.
        var firstSocket = this.sockets.First();
        _ = base.RunStreamingLoopAsync(firstSocket, stream, sessionId, configuration, cancellationToken);
    }

    private static NDArray Decode(byte[] buffer, int channels)
    {
        var array = Numpy.frombuffer(buffer, np.float32);
        var chunkLength = array.size / channels;
        var result = np.reshape(array, (chunkLength, channels));

        return result;
    }
}