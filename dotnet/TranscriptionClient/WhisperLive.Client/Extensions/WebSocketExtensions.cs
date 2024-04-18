using System.Text.Json;
using WebSocketSharp;
using WhisperLive.Abstraction.Configurations;

namespace WhisperLive.Client.Extensions;

internal static class WebSocketExtensions
{
    public static async Task InitiateConnectionAsync(
        this WebSocket socket,
        Guid sessionId,
        TranscriptorConfiguration configuration)
    {
        var serverReady = false;

        socket.OnMessage += ServerReadyListener;

        var json = JsonSerializer.Serialize(new
        {
            uid = sessionId,
            language = configuration.Language,
            task = "transcribe",
            model = configuration.Model,
            use_vad = configuration.UseVoiceActivityDetection,
            channel = 1,
            multilingual = configuration.IsMultiLanguage,
            options = configuration.Options,
        });
        socket.Send(json);

        while (!serverReady)
        {
            await Task.Delay(100);
        }

        socket.OnMessage -= ServerReadyListener;

        void ServerReadyListener(object sender, MessageEventArgs args)
        {
            var payload = JsonSerializer.Deserialize<JsonDocument>(args.Data, JsonSerializers.Default)!;

            if (payload.RootElement.TryGetProperty("message", out var v))
            {
                var rawText = v.GetString();

                if (rawText == "SERVER_READY")
                {
                    serverReady = true;
                }
            }
        }
    }

    public static void CloseConnection(this WebSocket socket)
    {
        if (socket.IsAlive)
        {
            socket.Send("END_OF_AUDIO"u8.ToArray());
            socket.Close(CloseStatusCode.Normal);
        }
    }
}