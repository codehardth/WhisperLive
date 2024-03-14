using Transcriptor.Py.Wrapper.Enums;
using Transcriptor.Py.Wrapper.Implementation;

class Program
{
    public static async Task Main(string[] args)
    {
        var serviceUri = new Uri("ws://192.168.20.98:9090");
        using var transcriptor = new WhisperTranscriptor(serviceUri);

        var options = new WhisperTranscriptorOptions(
            ModelType.Default,
            ModelSize: "CodeHardThailand/whisper-th-medium-combined-ct2",
            Language: "th",
            IsMultiLanguage: false,
            NumberOfSpeaker: 1);
        var url = new Uri(
            "https://livestream.parliament.go.th/lives/playlist.m3u8");
        // await transcriptor.StartRecordAsync(url, options);

        transcriptor.MessageArrived += (sessionId, speaker, segments) =>
        {
            var text =
                segments.GroupBy(s => s.Start)
                    .OrderByDescending(g => g.Max(s => s.End))
                    .SelectMany(g => g)
                    .FirstOrDefault();

            if (text is not null)
            {
                Console.WriteLine($"[{text.End}]{speaker}: {text.Text}");
            }

            return Task.CompletedTask;
        };

        var cts = new CancellationTokenSource();

        var session =
            await transcriptor.TranscribeAsync("/home/deszolate/Downloads/test_resampled.wav", options, cts.Token);

        await Task.Delay(1000 * 60);

        await cts.CancelAsync();
    }
}