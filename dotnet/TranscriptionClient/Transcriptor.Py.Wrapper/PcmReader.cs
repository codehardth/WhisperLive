using FFMpegCore;
using FFMpegCore.Pipes;

namespace Transcriptor.Py.Wrapper;

public static class PcmReader
{
    const int SampleRate = 16_000;
    const int AudioChannel = 1;
    const string AudioCodec = "pcm_s16le";
    const string Format = "s16le";
    const int ChunkSize = 4096;

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

        var success = argument.ProcessSynchronously();
        if (!success)
        {
            throw new InvalidOperationException($"Unable to convert {filePath} to {nameof(PcmReader)}.");
        }

        ms.Seek(0, SeekOrigin.Begin);

        return ms;
    }

    public static async IAsyncEnumerable<byte[]> FromHls(
        Uri uri,
        int channels = AudioChannel,
        CancellationToken ct = default)
    {
        await using var ms = new QueueStream();
        var argument =
            FFMpegArguments.FromUrlInput(uri)
                .OutputToPipe(new StreamPipeSink(ms), options =>
                {
                    options.WithCustomArgument(
                        $"-f {Format} -acodec {AudioCodec} -ac {channels} -ar {SampleRate}");
                });

        var readHlsStreamTask = argument.CancellableThrough(ct).ProcessAsynchronously();

        var buffer = new byte[ChunkSize * AudioChannel * 2]; // 2 bytes per sample
        var bytesRead = -1;
        while (!ct.IsCancellationRequested)
        {
            var read = await ms.ReadAsync(buffer, 0, buffer.Length, ct);

            // Don't feed the data yet if there is no buffer available from the start
            if (read == 0 && bytesRead == -1)
            {
                continue;
            }

            bytesRead += read;

            yield return buffer;
        }

        await readHlsStreamTask;
    }
}