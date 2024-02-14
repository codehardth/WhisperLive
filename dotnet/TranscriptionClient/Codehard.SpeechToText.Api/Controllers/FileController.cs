using Microsoft.AspNetCore.Mvc;

namespace Codehard.SpeechToText.Api.Controllers;

[Route("api/files")]
[ApiController]
public class FileController : Controller
{
    private readonly FileStorage _fileStorage;

    public FileController(FileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }

    [HttpPost]
    [RequestSizeLimit(1_000_000_000)]
    public async Task<IActionResult> UploadFileAsync(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream();

        var fileId = await _fileStorage.UploadAsync(stream, cancellationToken);

        return this.Ok(fileId);
    }
}