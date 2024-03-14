using System.Runtime.CompilerServices;
using System.Text;

namespace Transcriptor.Py.Wrapper;

public sealed class WaveFileReader : Stream
{
    private readonly BinaryReader reader;

    private int _dataChunkSize { get; init; }

    public string ChunkId { get; private init; }
    public int ChunkSize { get; private init; }
    public string Format { get; private init; }
    public string AudioFormat { get; private init; }
    public int NumChannels { get; private init; }
    public int SampleRate { get; private init; }
    public int ByteRate { get; private init; }
    public int BlockAlign { get; private init; }
    public int BitsPerSample { get; private init; }

    public override bool CanRead => true;
    public override bool CanSeek => this.reader.BaseStream.CanSeek;
    public override bool CanWrite => false;
    public override long Length { get; }
    public override long Position { get; set; }

    public WaveFileReader(string filePath)
        : this(File.ReadAllBytes(filePath))
    {
    }

    public WaveFileReader(byte[] data)
        : this(new MemoryStream(data))
    {
    }

    public WaveFileReader(Stream stream)
    {
        var reader = new BinaryReader(stream);

        var riffChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
        var riffChunkSize = reader.ReadInt32();
        var waveFormat = Encoding.ASCII.GetString(reader.ReadBytes(4));
        var nextSection = Encoding.ASCII.GetString(reader.ReadBytes(4));

        if (nextSection == "JUNK")
        {
            var junkChunkSize = reader.ReadInt32();
            var disposedJunkChunk = reader.ReadBytes(28);
            nextSection = Encoding.ASCII.GetString(reader.ReadBytes(4));
        }

        if (nextSection != "fmt ")
        {
            throw new NotImplementedException();
        }

        var fmtChunkId = nextSection;
        // Size of this section
        var fmtChunkSize = reader.ReadInt32();
        // PCM = 1, If it is other than 1 it means that the file is compressed.
        var audioFormat = reader.ReadInt16() == 1 ? "PCM" : "Compressed";
        // Mono = 1; Stereo = 2
        var numberOfChannels = reader.ReadInt16();
        // 8000, 44100, etc.
        var samplesPerSecond = reader.ReadInt32();
        // Byte Rate = SampleRate * NumChannels * BitsPerSample/8
        var avgBytePerSecond = reader.ReadInt32();
        // Block Align = NumChannels * BitsPerSample / 8
        var blockAlign = reader.ReadInt16();
        // 8 bits = 8, 16 bits = 16, etc.
        var bitsPerSample = reader.ReadInt16();

        if (fmtChunkSize == 18)
        {
            // size of the extension: 0
            var extensionSize = reader.ReadInt16();
        }
        else if (fmtChunkSize == 40)
        {
            // size of the extension: 22
            var extensionSize = reader.ReadInt16();
            // Should be lower or equal to bitsPerSample
            var validBitsPerSample = reader.ReadInt16();
            // Speaker position mask
            var channelMask = reader.ReadInt32();
            // GUID(first two bytes are the data format code)
            var subFormat = reader.ReadBytes(16);
        }
        else if (fmtChunkSize != 16)
        {
            throw new Exception("Invalid .wav format!");
        }

        nextSection = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (nextSection == "fact")
        {
            // Chunk size: 4
            var factChunkSize = reader.ReadInt32();
            // length of the sample
            var sampleLength = reader.ReadInt32();
            // Check what is the following section
            nextSection = Encoding.ASCII.GetString(reader.ReadBytes(4));
        }
        else if (nextSection != "data")
        {
            throw new NotImplementedException();
        }

        // Contains the letters "data"
        var dataChunkId = nextSection;
        // This is the number of bytes in the data.
        var dataChunkSize = reader.ReadInt32();

        this.reader = reader;
        this.ChunkId = fmtChunkId;
        this.ChunkSize = fmtChunkSize;
        this.Format = waveFormat;
        this.AudioFormat = audioFormat;
        this.NumChannels = numberOfChannels;
        this.SampleRate = samplesPerSecond;
        this.ByteRate = avgBytePerSecond;
        this.BlockAlign = blockAlign;
        this.BitsPerSample = bitsPerSample;
        this.Length = stream.Length;
        this._dataChunkSize = dataChunkSize;
    }

    public async IAsyncEnumerable<byte[]> IterateAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        this.Seek(44, SeekOrigin.Begin);

        var buffer = new byte[4096];
        while (await this.ReadAsync(buffer, cancellationToken) > 0)
        {
            yield return buffer;
        }
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        var currentPosition = this.Position;
        this.reader.BaseStream.Seek(0, SeekOrigin.Begin);

        var buffer = new byte[44];
        var metadataBufferSize =
            reader.BaseStream.Read(buffer, 0, 44);
        destination.WriteAsync(buffer, 0, metadataBufferSize);

        var remainingBuffer = new byte[bufferSize];

        int bytesRead;
        while ((bytesRead = reader.BaseStream.Read(
                   remainingBuffer, 0, bufferSize)) > 0)
        {
            destination.WriteAsync(remainingBuffer.AsMemory(0, bytesRead));
        }

        this.reader.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
    }

    public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        var currentPosition = this.Position;
        this.reader.BaseStream.Seek(0, SeekOrigin.Begin);

        var buffer = new byte[44];
        var metadataBufferSize =
            await reader.BaseStream.ReadAsync(buffer.AsMemory(0, 44), cancellationToken);
        await destination.WriteAsync(buffer.AsMemory(0, metadataBufferSize), cancellationToken);

        var remainingBuffer = new byte[bufferSize];

        int bytesRead;
        while ((bytesRead = await reader.BaseStream.ReadAsync(
                   remainingBuffer.AsMemory(0, bufferSize), cancellationToken)) > 0)
        {
            await destination.WriteAsync(remainingBuffer.AsMemory(0, bytesRead), cancellationToken);
        }

        this.reader.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var bytesRead = reader.Read(buffer, offset, count);
        Position += bytesRead;
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (this.CanSeek)
        {
            return this.reader.BaseStream.Seek(offset, origin);
        }

        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}