using Docker.DotNet;
using Runner;
using Transcriptor.Py.Wrapper.Abstraction;
using Transcriptor.Py.Wrapper.Configurations;
using Transcriptor.Py.Wrapper.Enums;
using Transcriptor.Py.Wrapper.Implementation;
using Transcriptor.Py.Wrapper.Models;

class Program
{
    public static async Task Main(string[] args)
    {
        var serviceUri = new Uri("ws://0.0.0.0:9999");

        using var dockerClient =
            new DockerClientConfiguration(new Uri("http://127.0.0.1:2375"))
                .CreateClient();
        var managerOptions = new TranscriptionManagerOptions(
            new Uri("ws://0.0.0.0"),
            "/home/deszolate/Documents/WhisperLive/images");
        var manager = new TranscriptionServerManager(dockerClient, managerOptions);

        // var id1 = await manager.StartInstanceAsync(9999, "gpu");
        // var id2 = await manager.StartInstanceAsync(10000, "gpu");
        // var id3 = await manager.StartInstanceAsync(10001, "gpu");
        //
        // await Task.Delay(3000);
        //
        // await manager.StopInstanceAsync(id1.Id);
        // await manager.StopInstanceAsync(id2.Id);
        // await manager.StopInstanceAsync(id3.Id);

        // return;

        // var serviceUri = new Uri("ws://192.168.20.98:9090");
        using var transcriptor = new WhisperTranscriptor(manager);

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
            useVoiceActivityDetection: true,
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
            Console.WriteLine($"{speaker}: {joinedText}");

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

        using var session = await transcriptor.TranscribeAsync(filePath, options, CancellationToken.None);

        Console.WriteLine("Press enter key to stop");
        Console.ReadLine();

        await transcriptor.StopAsync(session.Id, CancellationToken.None);
    }
}