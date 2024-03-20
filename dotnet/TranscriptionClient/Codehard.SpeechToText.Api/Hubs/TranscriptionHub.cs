using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Codehard.SpeechToText.Api.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Transcriptor.Py.Wrapper.Abstraction;
using Transcriptor.Py.Wrapper.Configurations;
using Transcriptor.Py.Wrapper.Enums;
using Transcriptor.Py.Wrapper.Implementation;

namespace Codehard.SpeechToText.Api.Hubs;

public class TranscriptionHub : Hub, IDiscoverableHub
{
    private readonly FileStorage _fileStorage;
    private readonly ITranscriptor _transcriptor;
    public static string Route => "/transcription";

    public TranscriptionHub(
        FileStorage fileStorage,
        ITranscriptor transcriptor)
    {
        _fileStorage = fileStorage;
        this._transcriptor = transcriptor;
    }

    [HubMethodName("file")]
    public async IAsyncEnumerable<string> TranscribeWithFileAsync(
        Guid fileId,
        string modelSize,
        string? language,
        bool multiLang,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var fileInfo = _fileStorage.GetFileInfo(fileId);
        if (fileInfo is null)
        {
            yield break;
        }

        var options =
            new WhisperTranscriptorOptions(modelSize, language, multiLang, TimeSpan.FromMilliseconds(100));
        using var session = await this._transcriptor.TranscribeAsync(fileInfo.FullName, options, cancellationToken);

        foreach (var result in this._transcriptor.Distinct())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            foreach (var message in result.Messages)
            {
                yield return message.Text;
            }
        }

        await this._transcriptor.StopAsync(session.Id, CancellationToken.None);
    }

    [HubMethodName("stream")]
    public async IAsyncEnumerable<string> TranscribeFromHlsStreamingAsync(
        Uri uri,
        string modelSize,
        string? language,
        bool multiLang,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options =
            new WhisperTranscriptorOptions(modelSize, language, multiLang, TimeSpan.FromMilliseconds(100));
        using var session = await this._transcriptor.TranscribeAsync(uri, options, cancellationToken);

        foreach (var result in this._transcriptor.Distinct())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            foreach (var message in result.Messages)
            {
                yield return message.Text;
            }
        }

        await this._transcriptor.StopAsync(session.Id, CancellationToken.None);
    }
}