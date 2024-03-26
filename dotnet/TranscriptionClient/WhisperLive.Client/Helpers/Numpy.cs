using NumSharp;
using NumSharp.Utilities;

namespace WhisperLive.Client.Helpers;

public static class Numpy
{
    public static NDArray bytes_to_float_array(byte[] audioBytes)
    {
        var rawData = frombuffer(audioBytes, np.int16);
        return rawData.astype(np.float32) / 32768.0;
    }

    public static NDArray frombuffer(byte[] buffer, Type type)
    {
        if (type == typeof(short))
        {
            return CreateInt16NdArrayFromBuffer(buffer);
        }

        return np.frombuffer(buffer, type);
    }

    public static NDArray frombuffer(byte[] buffer, string type)
    {
        if (type == nameof(Int16))
        {
            return CreateInt16NdArrayFromBuffer(buffer);
        }

        return np.frombuffer(buffer, type);
    }

    private static NDArray CreateInt16NdArrayFromBuffer(byte[] bytes)
    {
        var length = bytes.Length / InfoOf<short>.Size;
        var values = new int[length];
        for (var index = 0; index < length; ++index)
            values[index] = BitConverter.ToInt16(bytes, index * InfoOf<short>.Size);
        return new NDArray(values);
    }
}