using AIReception.Mvc.Models.Dms;
using AIReception.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIReception.Mvc.Controllers;

[ApiController]
public class NotesController : ControllerBase
{
    private readonly NotesService _notesService;

    public NotesController(NotesService notesService)
    {
        _notesService = notesService;
    }

    [HttpGet("/api/notes")]
    public IActionResult GetNotes([FromQuery] Guid? customerId, [FromQuery] Guid? vehicleId, [FromQuery] string? callSid)
    {
        return Ok(new { notes = _notesService.GetNotes(customerId, vehicleId, callSid) });
    }

    [HttpPost("/api/notes")]
    public IActionResult CreateNote([FromBody] CreateNoteRequest request)
    {
        return Ok(_notesService.CreateNote(request));
    }
}
