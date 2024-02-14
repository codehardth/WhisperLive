namespace Codehard.SpeechToText.Api;

public sealed class FileStorage
{
    private static readonly string FileDirectory = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "_files");

    public FileStorage()
    {
        System.IO.Directory.CreateDirectory(FileDirectory);
    }

    public async Task<Guid> UploadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var fileId = Guid.NewGuid();
        var fullPath = Path.Combine(FileDirectory, fileId.ToString());

        await using var fs = File.Create(fullPath);
        stream.Seek(0, SeekOrigin.Begin);
        await stream.CopyToAsync(fs, cancellationToken);

        return fileId;
    }

    public FileInfo? GetFileInfo(Guid fileId)
    {
        var fullPath = Path.Combine(FileDirectory, fileId.ToString());

        return !File.Exists(fullPath) ? default : new FileInfo(fullPath);
    }

    public Task<bool> TryReadAsync(Guid fileId, out Stream stream)
    {
        var fullPath = Path.Combine(FileDirectory, fileId.ToString());

        if (!File.Exists(fullPath))
        {
            stream = null!;
            return Task.FromResult(false);
        }

        stream = File.OpenRead(fullPath);
        return Task.FromResult(true);
    }
}