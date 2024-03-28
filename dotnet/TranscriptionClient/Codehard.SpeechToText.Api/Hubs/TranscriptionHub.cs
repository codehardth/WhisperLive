using System.Runtime.CompilerServices;
using Codehard.SpeechToText.Api.Abstractions;
using Microsoft.AspNetCore.SignalR;
using WhisperLive.Abstraction;
using WhisperLive.Abstraction.Configurations;

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
        using var session = await this._transcriptor.StartAsync(fileInfo.FullName, options, cancellationToken);

        yield return string.Empty;

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
        using var session = await this._transcriptor.StartAsync(uri, options, cancellationToken);

        yield return string.Empty;

        await this._transcriptor.StopAsync(session.Id, CancellationToken.None);
    }
}