using WhisperLive.Abstraction;

namespace WhisperLive.Coordinator.Docker;

public class DockerEngineCoordinator : ITranscriptionServerCoordinator
{
    private readonly ITranscriptionServerManager manager;

    public DockerEngineCoordinator(ITranscriptionServerManager manager)
    {
        this.manager = manager;
    }

    public async Task<Uri> GetTranscriptionServerEndpointAsync(
        object flag,
        CancellationToken cancellationToken = default)
    {
        if (flag is not int port)
        {
            throw new NotSupportedException();
        }

        var info = await manager.StartInstanceAsync(port, "gpu", cancellationToken);

        return info.Endpoint;
    }
}