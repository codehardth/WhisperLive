using System.Reactive.Linq;
using Runner;
using Transcriptor.Py.Wrapper.Abstraction;
using Transcriptor.Py.Wrapper.Enums;
using Transcriptor.Py.Wrapper.Implementation;
using Transcriptor.Py.Wrapper.Models;

class Program
{
    public static async Task Main(string[] args)
    {
        var serviceUri = new Uri("ws://192.168.20.98:9091");
        // var serviceUri = new Uri("ws://192.168.20.98:9090");
        using var transcriptor = new WhisperTranscriptor(serviceUri);

        var historyFilter = new HistoryMaintainerFilter();

        ISegmentPipeline filterPipeline = new SegmentPipeline();
        filterPipeline.AddFilter(historyFilter);
        filterPipeline.AddFilter<RemoveUnwantedWordsFilter>();
        filterPipeline.AddFilter<LastSegmentPerStartTimeFilter>();

        var options = new WhisperTranscriptorOptions(
            modelType: ModelType.Default,
            model: "CodeHardThailand/whisper-th-medium-combined-ct2",
            language: "en",
            forcedAudioChannels: 1,
            isMultiLanguage: false,
            useVoiceActivityDetection: false,
            transcriptionDelay: TimeSpan.FromMilliseconds(100),
            transcriptionTimeout: TimeSpan.FromSeconds(30),
            segmentFilter: filterPipeline);
        var url = new Uri(
            "https://livestream.parliament.go.th/lives/playlist.m3u8");
        // await transcriptor.StartRecordAsync(url, options);

        var cts = new CancellationTokenSource();

        var buffer = new Dictionary<double, Segment>();

        transcriptor.MessageArrived += (sessionId, speaker, segments) =>
        {
            Console.WriteLine("-------------------------------------");

            var joinedText = string.Join(" ", segments.Select(s => s.Text));
            Console.WriteLine(joinedText);

            return Task.CompletedTask;
        };

        transcriptor.SessionEnded += (id, reason) =>
        {
            Console.WriteLine($"{id} ended with {reason}");

            cts.Cancel();

            return Task.CompletedTask;
        };

        var filePath = "/home/deszolate/Downloads/yesterday.mp4";
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