using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using FFMpegCore;
using NAudio.Wave;

namespace Transcriptor;

public class TranscriptionClient
{
    private const int SampleRate = 16_000;

    public TranscriptionClient()
    {
    }

    public async Task TranscribeAsync(string audioFilePath)
    {
        var isWaveFile = audioFilePath.EndsWith(".wav");

        string processingFilePath = default!;

        try
        {
            if (isWaveFile)
            {
                processingFilePath = audioFilePath;
            }
            else
            {
                var outputWaveFilePath = Path.GetTempFileName().Replace(".tmp", ".wav");

                await ResampleAudioAsync(audioFilePath, outputWaveFilePath, SampleRate);

                processingFilePath = outputWaveFilePath;
            }

            Console.WriteLine($"Resample completed: {processingFilePath}");

            await StreamToTranscriptionServerAsync(processingFilePath);
        }
        finally
        {
            File.Delete(processingFilePath);
        }
    }

    private async Task StreamToTranscriptionServerAsync(string wavFilePath)
    {
        const int chunkSize = 1024;

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri("ws://192.168.0.98:9090"), CancellationToken.None);

        var sharedByteArrayPool = ArrayPool<byte>.Shared;
        var sharedShortArrayPool = ArrayPool<short>.Shared;

        var task = Task.Run((Func<Task>)(async () =>
        {
            while (ws.State == WebSocketState.Open)
            {
                var responseBuffer = new byte[1000];
                var res = await ws.ReceiveAsync(responseBuffer, CancellationToken.None);
                var resText = Encoding.UTF8.GetString(responseBuffer);

                Console.WriteLine(resText);
            }
        }));

        await using var reader = new WaveFileReader(wavFilePath);

        var sampleRate = reader.WaveFormat.BitsPerSample;

        var buffer = new byte[chunkSize];
        int bytesRead;

        while ((bytesRead = reader.Read(buffer, 0, chunkSize)) > 0)
        {
            // var sampleBuffer = sharedByteArrayPool.Rent(bytesRead / 2);

            try
            {
                // Buffer.BlockCopy(buffer, 0, sampleBuffer, 0, bytesRead);
            }
            catch (Exception exception)
            {
            }
            finally
            {
                // sharedByteArrayPool.Return(sampleBuffer);
            }

            var audioArray = BytesToFloatArray(buffer);

            var isEndOfStream = reader.Position == reader.Length;
            var flag = isEndOfStream ? WebSocketMessageFlags.EndOfMessage : WebSocketMessageFlags.None;
            var message = isEndOfStream ? Array.Empty<byte>() : buffer;

            await ws.SendAsync(message, WebSocketMessageType.Binary, flag, CancellationToken.None);

            // ws.SendAsync()
        }

        // var buffer = new byte[reader.Length];
        // var read = reader.Read(buffer, 0, buffer.Length);
        // var sampleBuffer = new short[read / 2];
        // Buffer.BlockCopy(buffer, 0, sampleBuffer, 0, read);

        // await using var fs = File.OpenRead(wavFilePath);
        // using var reader = new BinaryReader(fs);
        //
        // var buffer = new byte[1024];
        //
        // // var byteCount = 0;
        // while (reader.Read(buffer) > 0)
        // {
        //     var floatArray = BytesToFloatArray(buffer);
        // }

        // ArrayPool<byte>.Shared.Return(sharedBuffer);
    }

    private static float[] BytesToFloatArray(byte[] audioBytes)
    {
        // Convert audio data from bytes to a float array.
        // Assumes 16-bit PCM format and normalizes values between -1 and 1.
        var rawShortData = audioBytes.Select(b => (short)((b << 8) | b)).ToArray();
        var rawFloatData = Array.ConvertAll(rawShortData, value => value / 32768.0f);

        return rawFloatData;
    }

    private static async Task ResampleAudioAsync(string inputFile, string outputFile, int sampleRate)
    {
        await FFMpegArguments.FromFileInput(inputFile)
            .OutputToFile(outputFile, true, options => { options.WithAudioSamplingRate(sampleRate); })
            .ProcessAsynchronously();
    }
}