using Transcriptor.Py.Wrapper.Implementation;
using Transcriptor.Py.Wrapper.Models;

namespace Transcriptor.Py.Wrapper.Abstraction;

public delegate void TranscriptorReadyEventHandler(ITranscriptor sender);

public interface ITranscriptor : IObservable<TranscriptResult>, IDisposable
{
    IAsyncEnumerable<InputInterface> GetInputInterfacesAsync(CancellationToken cancellationToken = default);

    Task StartRecordAsync(
        int index,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default);

    Task StartRecordAsync(
        Uri uri,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task TranscriptAsync(
        string filePath,
        WhisperTranscriptorOptions options,
        CancellationToken cancellationToken = default);

    event TranscriptorReadyEventHandler TranscriptorReady;
}