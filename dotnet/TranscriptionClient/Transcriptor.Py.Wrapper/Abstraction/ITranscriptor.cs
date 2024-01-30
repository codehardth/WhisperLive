using Transcriptor.Py.Wrapper.Models;

namespace Transcriptor.Py.Wrapper.Abstraction;

public delegate void TranscriptorReadyEventHandler(ITranscriptor sender);

public interface ITranscriptor : IObservable<TranscriptMessage>, IDisposable
{
    IAsyncEnumerable<InputInterface> GetInputInterfacesAsync(CancellationToken cancellationToken = default);

    Task StartAsync(int index, CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    event TranscriptorReadyEventHandler TranscriptorReady;
}