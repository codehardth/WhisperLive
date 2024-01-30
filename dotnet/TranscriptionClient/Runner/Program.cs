using System.Reactive.Linq;
using Transcriptor.Py.Wrapper.Implementation;

class Program
{
    public static async Task Main(string[] args)
    {
        var serviceUri = new Uri("ws://localhost:8765");
        using var transcriptor = new WhisperTranscriptor(serviceUri);

        var devices = await transcriptor.GetInputInterfacesAsync().ToListAsync();
        var pulseDevice = devices.Single(d => d.Name == "default");

        await transcriptor.StartAsync(pulseDevice.Index);

        transcriptor
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