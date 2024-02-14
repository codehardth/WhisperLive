using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Codehard.SpeechToText.Api.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Transcriptor.Py.Wrapper.Abstraction;
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
        ModelType model,
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

        var options = new WhisperTranscriptorOptions(model, modelSize, language, multiLang);
        await this._transcriptor.TranscriptAsync(fileInfo.FullName, options, cancellationToken);

        foreach (var message in this._transcriptor.Distinct())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            yield return message.Text;
        }

        await this._transcriptor.StopAsync(CancellationToken.None);
    }

    [HubMethodName("microphone")]
    public async IAsyncEnumerable<string> TranscribeFromInputDeviceAsync(
        int deviceIndex,
        ModelType model,
        string modelSize,
        string? language,
        bool multiLang,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = new WhisperTranscriptorOptions(model, modelSize, language, multiLang);
        await this._transcriptor.StartRecordAsync(deviceIndex, options, cancellationToken);

        foreach (var message in this._transcriptor.Distinct())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            yield return message.Text;
        }

        await this._transcriptor.StopAsync(CancellationToken.None);
    }

    [HubMethodName("stream")]
    public async IAsyncEnumerable<string> TranscribeFromHlsStreamingAsync(
        Uri uri,
        ModelType model,
        string modelSize,
        string? language,
        bool multiLang,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = new WhisperTranscriptorOptions(model, modelSize, language, multiLang);
        await this._transcriptor.StartRecordAsync(uri, options, cancellationToken);

        foreach (var message in this._transcriptor.Distinct())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            yield return message.Text;
        }

        await this._transcriptor.StopAsync(CancellationToken.None);
    }
}