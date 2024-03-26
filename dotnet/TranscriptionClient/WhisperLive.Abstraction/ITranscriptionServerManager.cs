using WhisperLive.Abstraction.Models;

namespace WhisperLive.Abstraction;

public interface ITranscriptionServerManager
{
    Task<AsrInstanceInfo> StartInstanceAsync(int port, string tag = "latest", CancellationToken cancellationToken = default);

    Task StopInstanceAsync(string id, CancellationToken cancellationToken = default);

    Task RemoveInstanceAsync(string id, CancellationToken cancellationToken = default);
}