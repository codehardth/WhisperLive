using System.Reactive.Linq;
using Transcriptor.Py.Wrapper.Enums;
using Transcriptor.Py.Wrapper.Implementation;

class Program
{
    public static async Task Main(string[] args)
    {
        var serviceUri = new Uri("ws://192.168.0.98:8765");
        using var transcriptor = new WhisperTranscriptor(serviceUri);

        var devices = await transcriptor.GetInputInterfacesAsync().ToListAsync();
        var pulseDevice = devices.Single(d => d.Name == "MacBook Pro Microphone");
        var options = new WhisperTranscriptorOptions(
            ModelType.WhisperX, ModelSize: "large-v2", Language: "th", IsMultiLanguage: false);
        var url = new Uri(
            "https://cdn-live.tpchannel.org/v1/0180e10a4a7809df73070d7d8760/0180e10adac40b8ed59433d5f3ce/main.m3u8");
        await transcriptor.StartRecordAsync(url, options);

        transcriptor
            .SelectMany(m => m.Messages)
            .Select(m => m.Text)
            .Subscribe(Console.WriteLine);

        await Task.Delay(-1);
    }

    private static float[] BytesToFloatArray(byte[] audioBytes)
    {
        // Convert audio data from bytes to a float array
        // Assumes that the audio data is in 16-bit PCM format
        // Normalizes the audio data to have values between -1 and 1

        var rawShortData = audioBytes.Select(b => BitConverter.ToInt16([b, 0], 0)).ToArray();
        var rawFloatData = Array.ConvertAll(rawShortData, s => s / 32768.0f);

        return rawFloatData;
    }
}