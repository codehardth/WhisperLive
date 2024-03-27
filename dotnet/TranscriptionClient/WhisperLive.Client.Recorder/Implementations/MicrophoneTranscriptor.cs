using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using PortAudioSharp;
using WhisperLive.Abstraction.Configurations;
using WhisperLive.Abstraction.Models;
using WhisperLive.Client.Implementation;
using WhisperLive.Client.Recorder.Abstractions;
using WhisperLive.Client.Recorder.Models;

namespace WhisperLive.Client.Recorder.Implementations;

public class MicrophoneTranscriptor : SingleChannelTranscriptor, IMicrophoneTranscriptor
{
    public MicrophoneTranscriptor(Uri endpoint) : base(endpoint)
    {
        PortAudio.Initialize();
    }

    public IEnumerable<RecordDevice> GetCaptureDevices()
    {
        for (var i = 0; i != PortAudio.DeviceCount; ++i)
        {
            var deviceInfo = PortAudio.GetDeviceInfo(i);
            yield return new RecordDevice(i, deviceInfo.name);
        }
    }

    public async Task<TranscriptionSession> TranscribeAsync(
        RecordDevice device,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid();

        await InternalTranscribeAsync(
            ct => ReadFromMicrophoneAsync(device, options, ct),
            sessionId,
            1,
            options,
            cancellationToken);

        return new TranscriptionSession(sessionId, default);
    }

    private async IAsyncEnumerable<byte[]> ReadFromMicrophoneAsync(
        RecordDevice device,
        WhisperTranscriptorOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const int sampleRate = 16_000;
        const int bufferSize = 4096;

        var deviceInfo = PortAudio.GetDeviceInfo(device.Index);

        var chan = Channel.CreateBounded<byte[]>(1);
        var reader = chan.Reader;
        var writer = chan.Writer;

        var param = new StreamParameters();
        param.device = device.Index;
        param.channelCount = 1;
        param.sampleFormat = SampleFormat.Int16;
        param.suggestedLatency = deviceInfo.defaultLowInputLatency;
        param.hostApiSpecificStreamInfo = IntPtr.Zero;

        var stream = new PortAudioSharp.Stream(
            inParams: param,
            outParams: null,
            sampleRate: sampleRate,
            framesPerBuffer: bufferSize,
            streamFlags: StreamFlags.NoFlag,
            callback: Callback,
            userData: IntPtr.Zero
        );

        stream.Start();

        try
        {
            while (await reader.WaitToReadAsync(cancellationToken))
            {
                var buffer = await reader.ReadAsync(cancellationToken);

                yield return buffer;
            }
        }
        finally
        {
            stream.Stop();
        }

        StreamCallbackResult Callback(
            IntPtr input,
            IntPtr output,
            uint frameCount,
            ref StreamCallbackTimeInfo timeInfo,
            StreamCallbackFlags statusFlags,
            IntPtr userData)
        {
            if (frameCount == 0 || cancellationToken.IsCancellationRequested)
            {
                writer.Complete();

                return StreamCallbackResult.Abort;
            }

            var bufferSize = (int)frameCount * 2; // 2 bytes per sample
            var samples = new byte[bufferSize];
            Marshal.Copy(input, samples, 0, bufferSize);

            writer.TryWrite(samples);

            return StreamCallbackResult.Continue;
        }
    }

    protected override void Dispose(bool disposing)
    {
        PortAudio.Terminate();
        base.Dispose(disposing);
    }
}