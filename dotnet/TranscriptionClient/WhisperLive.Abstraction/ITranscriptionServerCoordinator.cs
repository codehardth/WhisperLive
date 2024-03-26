namespace WhisperLive.Abstraction;

public interface ITranscriptionServerCoordinator
{
    Task<Uri> GetTranscriptionServerEndpointAsync(object flag, CancellationToken cancellationToken = default);
}