using WhisperLive.Abstraction.Configurations;
using WhisperLive.Abstraction.Models;

namespace WhisperLive.Client.Implementation;

public class SingleChannelTranscriptor(Uri serviceUri) : WhisperTranscriptor
{
    protected readonly Uri ServiceUri = serviceUri;

    public override async Task<TranscriptionSession> TranscribeAsync(
        Uri uri,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid();

        await InternalTranscribeAsync(
            ct => PcmReader.FromHlsAsync(uri, 1, ct),
            sessionId,
            1,
            options,
            cancellationToken);

        return new TranscriptionSession(sessionId, default);
    }

    public override async Task<TranscriptionSession> TranscribeAsync(
        string filePath,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default)
    {
        var buffer = PcmReader.FromFile(filePath);

        var session = await TranscribeAsync(
            buffer,
            options,
            1,
            cancellationToken);

        return session;
    }

    protected override async Task ProcessStreamAsync(
        IAsyncEnumerable<byte[]> stream,
        Guid sessionId,
        int audioChannelCount,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken)
    {
        await OpenWebSocketConnectionAndStreamAudioAsync(
            this.ServiceUri,
            sessionId,
            stream,
            options,
            cancellationToken);
    }
}