using Microsoft.AspNetCore.Mvc;
using Transcriptor.Py.Wrapper.Abstraction;

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
        var devices = 
            this._transcriptor.GetInputInterfacesAsync(cancellationToken);

        return this.Ok(devices);
    }
}