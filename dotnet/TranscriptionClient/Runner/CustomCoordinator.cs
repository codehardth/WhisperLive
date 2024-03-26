using WhisperLive.Abstraction;

namespace Runner;

public class CustomCoordinator : ITranscriptionServerCoordinator
{
    private static readonly Dictionary<int, Uri> endpointMapper = new()
    {
        { 19090, new Uri("ws://192.168.20.118:9090") },
        { 19091, new Uri("ws://192.168.20.118:9091") },
    };

    public Task<Uri> GetTranscriptionServerEndpointAsync(object flag, CancellationToken cancellationToken = default)
    {
        if (flag is not int port)
        {
            throw new NotSupportedException();
        }

        return Task.FromResult(endpointMapper[port]);
    }
}