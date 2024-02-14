using Transcriptor.Py.Wrapper.Enums;

namespace Transcriptor.Py.Wrapper.Implementation;

public sealed record WhisperTranscriptorOptions(
    ModelType ModelType,
    string ModelSize,
    string? Language,
    bool IsMultiLanguage);