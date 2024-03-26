namespace WhisperLive.Coordinator.Docker.Configurations;

public record TranscriptionManagerOptions(
    string HostIp,
    string ImageCacheDirectory);