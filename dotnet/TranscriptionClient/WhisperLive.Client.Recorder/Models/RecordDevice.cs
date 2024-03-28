using PortAudioSharp;

namespace WhisperLive.Client.Recorder.Models;

public sealed record RecordDevice
{
    internal RecordDevice(int Index, string Name, int MaxInputChannels)
    {
        this.Index = Index;
        this.Name = Name;
        this.MaxInputChannels = MaxInputChannels;
    }

    internal DeviceInfo _deviceInfo { get; init; }

    public int Index { get; init; }
    public string Name { get; init; }
    public int MaxInputChannels { get; init; }
}