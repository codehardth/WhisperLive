using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using DemoApi.Extensions;
using Hangfire;
using MediatR;
using Transcriptor.Py.Wrapper.Abstraction;
using Transcriptor.Py.Wrapper.Implementation;

namespace DemoApi.Handlers;

public record MessageTranscribeStartRequest(int DeviceIndex) : IRequest<Guid>;

public record MessageTranscribeStopRequest(Guid SessionId) : IRequest;

public class MessageTranscribeHandler : IRequestHandler<MessageTranscribeStartRequest, Guid>,
    IRequestHandler<MessageTranscribeStopRequest>
{
    private static readonly ConcurrentDictionary<Guid, string> _taskPool = new();

    private readonly ITranscriptor _transcriptor;
    private readonly WhisperTranscriptorOptions _options;
    private readonly ILogger<MessageTranscribeHandler> _logger;

    public MessageTranscribeHandler(
        ITranscriptor transcriptor,
        WhisperTranscriptorOptions options,
        ILogger<MessageTranscribeHandler> logger)
    {
        _transcriptor = transcriptor;
        _options = options;
        _logger = logger;
    }

    public async Task<Guid> Handle(MessageTranscribeStartRequest request, CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid();
        var jobId = BackgroundJob.Enqueue(
            () => LongRunRecordAsync(sessionId, request.DeviceIndex, CancellationToken.None));

        _ = _taskPool.AddOrUpdate(sessionId, jobId, (_, _) => jobId);

        return sessionId;
    }

    public async Task LongRunRecordAsync(Guid sessionId, int index, CancellationToken cancellationToken)
    {
        try
        {
            await this._transcriptor.StartRecordAsync(index, this._options, cancellationToken);

            Action<string> log = m => this._logger.LogInformation($"[{sessionId}] {m}");

            this._transcriptor
                .SelectMany(m => m.Messages)
                .Select(m => m.Text)
                .Subscribe(log, cancellationToken);

            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await this._transcriptor.StopAsync(CancellationToken.None);

            this._logger.LogInformation("Record cancelled gracefully");
        }
    }

    public async Task Handle(MessageTranscribeStopRequest request, CancellationToken cancellationToken)
    {
        var taskExist = _taskPool.TryGetValue(request.SessionId, out var task);

        if (!taskExist || task is null)
        {
            return;
        }

        BackgroundJob.Delete(task);
        _taskPool.Remove(request.SessionId, out _);
    }
}