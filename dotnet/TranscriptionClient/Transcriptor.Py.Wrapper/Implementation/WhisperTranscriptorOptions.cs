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
        TimeSpan transcriptionDelay,
        TimeSpan transcriptionTimeout)
    {
        this.ModelType = modelType;
        this.Model = model;
        this.Language = language;
        this.ForcedAudioChannels = forcedAudioChannels;
        this.IsMultiLanguage = isMultiLanguage;
        this.TranscriptionDelay = transcriptionDelay;
        this.TranscriptionTimeout = transcriptionTimeout;
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
            transcriptionDelay,
            transcriptionTimeout)
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
            transcriptionDelay,
            TimeSpan.FromSeconds(30))
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
            TimeSpan.FromSeconds(300),
            TimeSpan.FromSeconds(30))
    {
    }

    public ModelType ModelType { get; init; }

    public string Model { get; init; }

    public string? Language { get; init; }

    public bool IsMultiLanguage { get; init; }

    public uint NumberOfSpeaker { get; init; }

    public int? ForcedAudioChannels { get; init; }

    public TimeSpan TranscriptionDelay { get; init; }

    public TimeSpan TranscriptionTimeout { get; init; }
}