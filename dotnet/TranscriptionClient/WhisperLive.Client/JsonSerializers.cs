using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhisperLive.Client;

internal static class JsonSerializers
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };
}