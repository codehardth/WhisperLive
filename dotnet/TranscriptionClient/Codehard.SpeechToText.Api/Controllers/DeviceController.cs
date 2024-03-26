using Microsoft.AspNetCore.Mvc;
using WhisperLive.Abstraction;

namespace Codehard.SpeechToText.Api.Controllers;

[Route("api/devices")]
[ApiController]
public class DeviceController : Controller
{
    private readonly ITranscriptor _transcriptor;

    public DeviceController(ITranscriptor transcriptor)
    {
        _transcriptor = transcriptor;
    }

    [HttpGet]
    public IActionResult GetDevices(CancellationToken cancellationToken = default)
    {
        return this.Ok(Array.Empty<int>());
    }
}