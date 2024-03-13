using Transcriptor.Py.Wrapper.Enums;

namespace Transcriptor.Py.Wrapper.Implementation;

public sealed record WhisperTranscriptorOptions
{
    public WhisperTranscriptorOptions(ModelType ModelType,
        string ModelSize,
        string? Language,
        bool IsMultiLanguage,
        uint NumberOfSpeaker)
    {
        this.ModelType = ModelType;
        this.ModelSize = ModelSize;
        this.Language = Language;
        this.IsMultiLanguage = IsMultiLanguage;
        this.NumberOfSpeaker = Math.Clamp(NumberOfSpeaker, 1, 4);
    }

    public ModelType ModelType { get; init; }
    public string ModelSize { get; init; }
    public string? Language { get; init; }
    public bool IsMultiLanguage { get; init; }
    public uint NumberOfSpeaker { get; init; }
}