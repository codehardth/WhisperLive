using Transcriptor.Py.Wrapper.Abstraction;
using Transcriptor.Py.Wrapper.Enums;

namespace Transcriptor.Py.Wrapper.Implementation;

public sealed record WhisperTranscriptorOptions
{
    public WhisperTranscriptorOptions(
        ModelType modelType,
        string model,
        string? language,
        bool isMultiLanguage,
        int? forcedAudioChannels,
        bool useVoiceActivityDetection,
        TimeSpan transcriptionDelay,
        TimeSpan transcriptionTimeout,
        ISegmentFilter? segmentFilter)
    {
        this.ModelType = modelType;
        this.Model = model;
        this.Language = language;
        this.ForcedAudioChannels = forcedAudioChannels;
        this.UseVoiceActivityDetection = useVoiceActivityDetection;
        this.IsMultiLanguage = isMultiLanguage;
        this.TranscriptionDelay = transcriptionDelay;
        this.TranscriptionTimeout = transcriptionTimeout;
        this.SegmentFilter = segmentFilter;
    }

    public WhisperTranscriptorOptions(
        string model,
        string? language,
        bool isMultiLanguage,
        TimeSpan transcriptionDelay,
        TimeSpan transcriptionTimeout)
        : this(
            ModelType.Default,
            model,
            language,
            isMultiLanguage,
            default,
            true,
            transcriptionDelay,
            transcriptionTimeout,
            default)
    {
    }

    public WhisperTranscriptorOptions(
        string model,
        string? language,
        bool isMultiLanguage,
        TimeSpan transcriptionDelay)
        : this(
            ModelType.Default,
            model,
            language,
            isMultiLanguage,
            default,
            true,
            transcriptionDelay,
            TimeSpan.FromSeconds(30),
            default)
    {
    }

    public WhisperTranscriptorOptions(
        string model,
        string? language,
        bool isMultiLanguage)
        : this(
            ModelType.Default,
            model,
            language,
            isMultiLanguage,
            default,
            true,
            TimeSpan.FromSeconds(300),
            TimeSpan.FromSeconds(30),
            default)
    {
    }

    public ModelType ModelType { get; init; }

    public string Model { get; init; }

    public string? Language { get; init; }

    public bool IsMultiLanguage { get; init; }

    public uint NumberOfSpeaker { get; init; }

    public int? ForcedAudioChannels { get; init; }

    public bool UseVoiceActivityDetection { get; set; }

    public TimeSpan TranscriptionDelay { get; init; }

    public TimeSpan TranscriptionTimeout { get; init; }

    public ISegmentFilter? SegmentFilter { get; set; }
}