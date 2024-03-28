using WhisperLive.Abstraction;

namespace WhisperLive.Client.Implementation;

public sealed class SingleServerCoordinator : ITranscriptionServerCoordinator
{
    private readonly Uri endpoint;

    public SingleServerCoordinator(Uri endpoint)
    {
        this.endpoint = endpoint;
    }

    public ValueTask<Uri> GetTranscriptionServerEndpointAsync(object flag, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(this.endpoint);
    }
}