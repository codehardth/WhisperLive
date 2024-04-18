using WhisperLive.Abstraction;
using WhisperLive.Abstraction.Configurations;
using WhisperLive.Abstraction.Models;
using WhisperLive.Client.Recorder.Models;

namespace WhisperLive.Client.Recorder.Abstractions;

public interface IMicrophoneTranscriptor : ITranscriptor
{
    IEnumerable<RecordDevice> GetCaptureDevices();

    Task<TranscriptionSession> StartAsync(
        RecordDevice device,
        TranscriptorConfiguration configuration,
        int? limitedAudioChannel = default,
        CancellationToken cancellationToken = default);
}