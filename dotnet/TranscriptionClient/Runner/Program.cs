using Transcriptor.Py.Wrapper.Enums;
using Transcriptor.Py.Wrapper.Implementation;
using Transcriptor.Py.Wrapper.Models;

class Program
{
    public static async Task Main(string[] args)
    {
        var serviceUri = new Uri("ws://192.168.20.98:9090");
        using var transcriptor = new WhisperTranscriptor(serviceUri);

        var options = new WhisperTranscriptorOptions(
            modelType: ModelType.Default,
            model: "CodeHardThailand/whisper-th-medium-combined-ct2",
            language: "en",
            forcedAudioChannels: 1,
            isMultiLanguage: false,
            transcriptionDelay: TimeSpan.FromMilliseconds(300),
            transcriptionTimeout: TimeSpan.FromSeconds(30));
        var url = new Uri(
            "https://livestream.parliament.go.th/lives/playlist.m3u8");
        // await transcriptor.StartRecordAsync(url, options);

        var cts = new CancellationTokenSource();

        var buffer = new Dictionary<double, Segment>();

        transcriptor.MessageArrived += (sessionId, speaker, segments) =>
        {
            var text =
                segments
                    .GroupBy(s => s.Start)
                    .OrderByDescending(g => g.Max(s => s.End))
                    .SelectMany(g => g)
                    .FirstOrDefault();

            if (text is not null)
            {
                Console.WriteLine($"[{text.End}]{speaker}: {text.Text}");
            }

            return Task.CompletedTask;
        };

        transcriptor.SessionEnded += (id, reason) =>
        {
            Console.WriteLine($"{id} ended with {reason}");

            cts.Cancel();

            return Task.CompletedTask;
        };

        var filePath = "/home/deszolate/Downloads/we_can_work_it_out.aac";
        // await using var stream = WaveFileReader.OpenRead(filePath);

        using var session = await transcriptor.TranscribeAsync(filePath, options with
        {
            NumberOfSpeaker = 1,
        }, CancellationToken.None);

        Console.WriteLine("Press enter key to stop");
        Console.ReadLine();

        await transcriptor.StopAsync(session.Id, CancellationToken.None);
    }
}