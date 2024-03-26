using Docker.DotNet;
using WhisperLive.Abstraction;
using WhisperLive.Abstraction.Configurations;
using WhisperLive.Client.Implementation;
using WhisperLive.Client.Recorder.Implementations;
using WhisperLive.Coordinator.Docker;
using WhisperLive.Coordinator.Docker.Configurations;

namespace Runner;

class Program
{
    public static async Task Main(string[] args)
    {
        var baseUri = "192.168.20.118";
        var serviceUri = new Uri($"ws://{baseUri}:9090");

        using var dockerClient =
            new DockerClientConfiguration(new Uri($"http://{baseUri}:2375"))
                .CreateClient();
        var managerOptions = new TranscriptionManagerOptions(
            "0.0.0.0",
            "/home/deszolate/Documents/WhisperLive/images");
        var manager = new TranscriptionServerManager(dockerClient, managerOptions);
        var coordinator = new CustomCoordinator();

        //
        // var res1 = await manager.StartInstanceAsync(45000, "gpu");
        // var res2 = await manager.StartInstanceAsync(45001, "gpu");
        //
        // await Task.Delay(1000);
        //
        // await manager.StopInstanceAsync(res1.Id);
        // await manager.StopInstanceAsync(res2.Id);
        //
        // return;

        // using var transcriptor = new MultiChannelTranscriptor(coordinator);
        // using var transcriptor = new SingleChannelTranscriptor(serviceUri);
        using var transcriptor = new MicrophoneTranscriptor(serviceUri);

        var historyFilter = new HistoryMaintainerFilter();

        ISegmentFilterPipeline filterFilterPipeline = new SegmentFilterFilterPipeline();
        // filterPipeline.AddFilter(historyFilter);
        // filterPipeline.AddFilter<RemoveUnwantedWordsFilter>();
        // filterFilterPipeline.AddFilter<LastSegmentPerStartTimeFilter>();

        var options = new WhisperTranscriptorOptions(
            model: "CodeHardThailand/whisper-th-medium-combined-ct2",
            language: "th",
            isMultiLanguage: false,
            useVoiceActivityDetection: false,
            transcriptionDelay: TimeSpan.FromMilliseconds(10),
            transcriptionTimeout: TimeSpan.FromSeconds(30),
            segmentFilter: filterFilterPipeline);
        var url = new Uri("https://livestream.parliament.go.th/lives/playlist.m3u8");
        // await transcriptor.StartRecordAsync(url, options);

        var cts = new CancellationTokenSource();

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

        var recordDevices = transcriptor.GetCaptureDevices().ToList();
        using var session = await transcriptor.TranscribeAsync(
            recordDevices.First(d => d.Name == "default"),
            options,
            CancellationToken.None);
        // using var session = await transcriptor.TranscribeAsync(url, options, CancellationToken.None);
        // using var session2 = await transcriptor2.TranscribeAsync(filePath, options with
        // {
        //     Language = "en",
        //     IsMultiLanguage = false,
        // }, CancellationToken.None);

        Console.WriteLine("Press enter key to stop");
        Console.ReadLine();

        await transcriptor.StopAsync(session.Id, CancellationToken.None);
        // await transcriptor2.StopAsync(session2.Id, CancellationToken.None);
    }

    enum CaptureMode
    {
        Capture = 1,

        // ReSharper disable once UnusedMember.Local
        LoopbackCapture = 2
    }
}