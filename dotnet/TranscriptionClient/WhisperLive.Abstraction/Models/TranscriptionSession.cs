namespace WhisperLive.Abstraction.Models;

public class TranscriptionSession : IDisposable
{
    private readonly IDisposable? _disposable;

    public TranscriptionSession(Guid id, IDisposable? disposable)
    {
        this._disposable = disposable;
        this.Id = id;
    }

    public Guid Id { get; }

    public void Dispose()
    {
        this._disposable?.Dispose();
    }
}