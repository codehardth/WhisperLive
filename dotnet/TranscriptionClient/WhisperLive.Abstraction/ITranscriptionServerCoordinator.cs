namespace WhisperLive.Abstraction;

public interface ITranscriptionServerCoordinator
{
    ValueTask<Uri> GetTranscriptionServerEndpointAsync(object flag, CancellationToken cancellationToken = default);
}