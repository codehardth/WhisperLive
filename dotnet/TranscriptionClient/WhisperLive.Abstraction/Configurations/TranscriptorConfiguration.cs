namespace WhisperLive.Abstraction.Configurations;

public sealed record TranscriptorConfiguration
{
    public TranscriptorConfiguration(
        string model,
        string? language,
        bool isMultiLanguage,
        bool useVoiceActivityDetection,
        TimeSpan transcriptionDelay,
        TimeSpan transcriptionTimeout,
        ISegmentFilter? segmentFilter,
        TranscriptionOptions options)
    {
        this.Model = model;
        this.Language = language;
        this.UseVoiceActivityDetection = useVoiceActivityDetection;
        this.IsMultiLanguage = isMultiLanguage;
        this.TranscriptionDelay = transcriptionDelay;
        this.TranscriptionTimeout = transcriptionTimeout;
        this.SegmentFilter = segmentFilter;
        this.Options = options;
    }

    public TranscriptorConfiguration(
        string model,
        string? language,
        bool isMultiLanguage,
        TimeSpan transcriptionDelay,
        TimeSpan transcriptionTimeout)
        : this(
            model,
            language,
            isMultiLanguage,
            true,
            transcriptionDelay,
            transcriptionTimeout,
            default,
            TranscriptionOptions.Default)
    {
    }

    public TranscriptorConfiguration(
        string model,
        string? language,
        bool isMultiLanguage,
        TimeSpan transcriptionDelay)
        : this(
            model,
            language,
            isMultiLanguage,
            true,
            transcriptionDelay,
            TimeSpan.FromSeconds(30),
            default,
            TranscriptionOptions.Default)
    {
    }

    public TranscriptorConfiguration(
        string model,
        string? language,
        bool isMultiLanguage)
        : this(
            model,
            language,
            isMultiLanguage,
            true,
            TimeSpan.FromSeconds(300),
            TimeSpan.FromSeconds(30),
            default,
            TranscriptionOptions.Default)
    {
    }

    public string Model { get; init; }

    public string? Language { get; init; }

    public bool IsMultiLanguage { get; init; }

    public bool UseVoiceActivityDetection { get; init; }

    public TimeSpan TranscriptionDelay { get; init; }

    public TimeSpan TranscriptionTimeout { get; init; }

    public ISegmentFilter? SegmentFilter { get; init; }

    public TranscriptionOptions Options { get; init; }
}