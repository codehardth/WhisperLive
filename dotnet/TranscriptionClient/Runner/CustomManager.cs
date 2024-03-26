using WhisperLive.Abstraction;
using WhisperLive.Abstraction.Models;

namespace Runner;

public class CustomManager : ITranscriptionServerManager
{
    private static readonly Dictionary<int, Uri> endpointMapper = new()
    {
        { 19090, new Uri("ws://192.168.20.98:19090") },
        { 19091, new Uri("ws://127.0.0.1:9090") },
    };

    public Task<AsrInstanceInfo> StartInstanceAsync(
        int port,
        string tag = "latest",
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AsrInstanceInfo(string.Empty, endpointMapper[port]));
    }

    public Task StopInstanceAsync(string id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task RemoveInstanceAsync(string id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}