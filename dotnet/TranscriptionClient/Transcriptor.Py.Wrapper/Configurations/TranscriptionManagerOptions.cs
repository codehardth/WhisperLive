namespace Transcriptor.Py.Wrapper.Configurations;

public record TranscriptionManagerOptions(
    Uri BaseEndpoint,
    string ImageCacheDirectory);