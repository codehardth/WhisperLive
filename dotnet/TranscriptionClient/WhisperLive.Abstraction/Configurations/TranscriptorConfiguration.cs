namespace WhisperLive.Abstraction.Configurations;

public sealed record TranscriptorConfiguration
{
    public TranscriptorConfiguration(
        string model,
        string? language,
        bool isMultiLanguage,
        bool useVoiceActivityDetection,
        int cpuThreads,
        TimeSpan transcriptionDelay,
        TimeSpan transcriptionTimeout,
        ISegmentFilter? segmentFilter,
        TranscriptionOptions options)
    {
        this.Model = string.IsNullOrWhiteSpace(model) ? "small.en" : model;
        this.Language = language;
        this.UseVoiceActivityDetection = useVoiceActivityDetection;
        this.IsMultiLanguage = isMultiLanguage;
        this.CpuThreads = cpuThreads;
        this.TranscriptionDelay = transcriptionDelay;
        this.TranscriptionTimeout = transcriptionTimeout;
        this.SegmentFilter = segmentFilter;
        this.Options = options;
    }

    public TranscriptorConfiguration(
        string model,
        string? language,
        bool isMultiLanguage,
        bool useVoiceActivityDetection,
        TimeSpan transcriptionDelay,
        TimeSpan transcriptionTimeout,
        ISegmentFilter? segmentFilter,
        TranscriptionOptions options)
        : this(
            model,
            language,
            isMultiLanguage,
            useVoiceActivityDetection,
            0,
            transcriptionDelay,
            transcriptionTimeout,
            segmentFilter,
            options)
    {
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
            0,
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
            0,
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
            0,
            TimeSpan.FromSeconds(300),
            TimeSpan.FromSeconds(30),
            default,
            TranscriptionOptions.Default)
    {
    }

    /// <summary>
    /// model (str, optional): The whisper model size. Defaults to 'small.en'
    /// </summary>
    public string Model { get; init; }

    /// <summary>
    /// language (str, optional): The language for transcription. Defaults to None.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// multilingual (bool, optional): Whether the client supports multilingual transcription. Defaults to False.
    /// </summary>
    public bool IsMultiLanguage { get; init; }

    public bool UseVoiceActivityDetection { get; init; }

    /// <summary>
    /// cpu_threads: Number of threads to use when running on CPU (4 by default).
    /// </summary>
    public int CpuThreads { get; init; }

    public TimeSpan TranscriptionDelay { get; init; }

    public TimeSpan TranscriptionTimeout { get; init; }

    public ISegmentFilter? SegmentFilter { get; init; }

    public TranscriptionOptions Options { get; init; }
}