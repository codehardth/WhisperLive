namespace Transcriptor.Py.Wrapper.Models;

public class TranscriptionSession : IDisposable
{
    private readonly IDisposable? _disposable;

    internal TranscriptionSession(Guid id, IDisposable? disposable)
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