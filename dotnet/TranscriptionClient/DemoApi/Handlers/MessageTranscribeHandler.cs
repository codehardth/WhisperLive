using System.Collections.Concurrent;
using System.Reactive.Linq;
using MediatR;
using Transcriptor.Py.Wrapper.Abstraction;

namespace DemoApi.Handlers;

public record MessageTranscribeStartRequest(int DeviceIndex) : IRequest<Guid>;

public class MessageTranscribeHandler : IRequestHandler<MessageTranscribeStartRequest, Guid>
{
    private static readonly ConcurrentDictionary<Guid, Task> _taskPool = new();

    private readonly ITranscriptor _transcriptor;
    private readonly ILogger<MessageTranscribeHandler> _logger;


    public MessageTranscribeHandler(
        ITranscriptor transcriptor,
        ILogger<MessageTranscribeHandler> logger)
    {
        _transcriptor = transcriptor;
        _logger = logger;
    }

    public async Task<Guid> Handle(MessageTranscribeStartRequest request, CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid();

        var task = Task.Run(async () =>
        {
            await this._transcriptor.StartAsync(request.DeviceIndex, cancellationToken);

            Action<string> log = m => this._logger.LogInformation($"[{sessionId}] {m}");

            this._transcriptor
                .Select(m => m.Text)
                .Subscribe(log);
        }, CancellationToken.None);

        _ = _taskPool.AddOrUpdate(sessionId, task, (_, _) => task);

        return sessionId;
    }
}