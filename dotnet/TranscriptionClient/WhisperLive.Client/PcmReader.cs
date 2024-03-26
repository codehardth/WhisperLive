using System.Runtime.CompilerServices;
using FFMpegCore;
using FFMpegCore.Pipes;

namespace WhisperLive.Client;

public static class PcmReader
{
    private const int SampleRate = 16_000;
    private const int AudioChannel = 1;
    private const string AudioCodec = "pcm_s16le";
    private const string Format = "s16le";
    private const int ChunkSize = 4096;

    public static MemoryStream FromFile(string filePath, int channels = AudioChannel)
    {
        var ms = new MemoryStream();
        var argument =
            FFMpegArguments.FromFileInput(filePath)
                .OutputToPipe(new StreamPipeSink(ms), options =>
                {
                    options.WithCustomArgument(
                        $"-f {Format} -acodec {AudioCodec} -ac {channels} -ar {SampleRate}");
                });

        bool success;

        retry:

        try
        {
            success = argument.ProcessSynchronously();
        }
        catch (ObjectDisposedException)
        {
            goto retry;
        }

        if (!success)
        {
            throw new InvalidOperationException($"Unable to convert {filePath} to {nameof(PcmReader)}.");
        }

        ms.Seek(0, SeekOrigin.Begin);

        return ms;
    }

    public static IAsyncEnumerable<byte[]> FromFileAsync(
        string filePath,
        int channels = AudioChannel,
        CancellationToken ct = default)
    {
        return InternalReadAsync<MemoryStream>(FFMpegArguments.FromFileInput(filePath), channels, ct);
    }

    public static IAsyncEnumerable<byte[]> FromHlsAsync(
        Uri uri,
        int channels = AudioChannel,
        CancellationToken ct = default)
    {
        return InternalReadAsync<QueueStream>(FFMpegArguments.FromUrlInput(uri), channels, ct);
    }

    private static async IAsyncEnumerable<byte[]> InternalReadAsync<TStream>(
        FFMpegArguments arguments,
        int channels,
        [EnumeratorCancellation] CancellationToken ct)
        where TStream : MemoryStream, new()
    {
        await using var bufferStream = new TStream();
        var argument =
            arguments.OutputToPipe(new StreamPipeSink(bufferStream), options =>
            {
                options.WithCustomArgument(
                    $"-f {Format} -acodec {AudioCodec} -ac {channels} -ar {SampleRate}");
            });

        var streamTask = argument.CancellableThrough(ct).ProcessAsynchronously().ConfigureAwait(false);

        var buffer = new byte[ChunkSize * AudioChannel * 2]; // 2 bytes per sample
        var bytesRead = -1;
        while (!ct.IsCancellationRequested)
        {
            var read = await bufferStream.ReadAsync(buffer, ct).ConfigureAwait(false);

            // Don't feed the data yet if there is no buffer available from the start
            if (read == 0 && bytesRead == -1)
            {
                continue;
            }

            bytesRead += read;

            yield return buffer;
        }

        await streamTask;
    }
}