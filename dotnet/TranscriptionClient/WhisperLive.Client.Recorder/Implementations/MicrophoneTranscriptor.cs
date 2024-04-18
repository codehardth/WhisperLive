using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using PortAudioSharp;
using WhisperLive.Abstraction;
using WhisperLive.Abstraction.Configurations;
using WhisperLive.Abstraction.Models;
using WhisperLive.Client.Implementation;
using WhisperLive.Client.Recorder.Abstractions;
using WhisperLive.Client.Recorder.Models;

namespace WhisperLive.Client.Recorder.Implementations;

public class MicrophoneTranscriptor : MultiChannelTranscriptor, IMicrophoneTranscriptor
{
    public MicrophoneTranscriptor(Uri endpoint)
        : this(new SingleServerCoordinator(endpoint))
    {
    }

    public MicrophoneTranscriptor(ITranscriptionServerCoordinator coordinator)
        : base(coordinator)
    {
        PortAudio.Initialize();
    }

    public IEnumerable<RecordDevice> GetCaptureDevices()
    {
        for (var i = 0; i != PortAudio.DeviceCount; ++i)
        {
            var deviceInfo = PortAudio.GetDeviceInfo(i);
            yield return new RecordDevice(i, deviceInfo.name, deviceInfo.maxInputChannels)
            {
                _deviceInfo = deviceInfo,
            };
        }
    }

    public async Task<TranscriptionSession> StartAsync(
        RecordDevice device,
        TranscriptorConfiguration configuration,
        int? limitedAudioChannel = default,
        CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid();

        var channelCount = limitedAudioChannel ?? device._deviceInfo.maxInputChannels;

        await InternalTranscribeAsync(
            ct => ReadFromMicrophoneAsync(device, channelCount, configuration, ct),
            sessionId,
            channelCount,
            configuration,
            cancellationToken);

        return new TranscriptionSession(sessionId, default);
    }

    private async IAsyncEnumerable<byte[]> ReadFromMicrophoneAsync(
        RecordDevice device,
        int channelCount,
        TranscriptorConfiguration _,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        const int sampleRate = 16_000;
        const int bufferSize = 4096;

        var inputChannels = channelCount;

        // 2 bytes per sample per channel
        var pool = ArrayPool<byte>.Create(
            maxArrayLength: bufferSize * inputChannels * 2,
            maxArraysPerBucket: 1);
        var chan = Channel.CreateBounded<byte[]>(capacity: 1);
        var reader = chan.Reader;
        var writer = chan.Writer;

        var param = new StreamParameters
        {
            device = device.Index,
            channelCount = inputChannels,
            sampleFormat = SampleFormat.Int16,
            suggestedLatency = device._deviceInfo.defaultLowInputLatency,
            hostApiSpecificStreamInfo = IntPtr.Zero,
        };

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

            var size = frameCount * inputChannels * 2;
            var buffer = pool.Rent((int)size);

            try
            {
                Marshal.Copy(input, buffer, 0, (int)size);
                writer.TryWrite(buffer);
            }
            finally
            {
                pool.Return(buffer);
            }

            return StreamCallbackResult.Continue;
        }
    }

    protected override void Dispose(bool disposing)
    {
        PortAudio.Terminate();
        base.Dispose(disposing);
    }
}