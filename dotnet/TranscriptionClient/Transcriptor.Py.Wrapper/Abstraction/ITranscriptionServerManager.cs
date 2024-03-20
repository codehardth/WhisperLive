namespace Transcriptor.Py.Wrapper.Abstraction;

public interface ITranscriptionServerManager
{
    Task<string> StartInstanceAsync(int port, string tag = "latest", CancellationToken cancellationToken = default);

    Task StopInstanceAsync(string id, CancellationToken cancellationToken = default);

    Task RemoveInstanceAsync(string id, CancellationToken cancellationToken = default);
}