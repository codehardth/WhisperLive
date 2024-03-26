namespace WhisperLive.Client;

internal sealed class QueueStream : MemoryStream
{
    private long _readPosition;
    private long _writePosition;

    public override int Read(byte[] buffer, int offset, int count)
    {
        this.Position = _readPosition;

        var temp = base.Read(buffer, offset, count);

        this._readPosition = Position;

        return temp;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        this.Position = _readPosition;

        var temp = await base.ReadAsync(buffer, offset, count, cancellationToken);

        this._readPosition = Position;

        return temp;
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        this.Position = _readPosition;

        var temp = await base.ReadAsync(buffer, cancellationToken);

        this._readPosition = Position;

        return temp;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        this.Position = _writePosition;

        base.Write(buffer, offset, count);

        this._writePosition = Position;
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        this.Position = _writePosition;

        await base.WriteAsync(buffer, offset, count, cancellationToken);

        this._writePosition = Position;
    }

    public override async ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        this.Position = _writePosition;

        await base.WriteAsync(buffer, cancellationToken);

        this._writePosition = Position;
    }
}