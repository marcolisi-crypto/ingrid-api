using AIReception.Mvc.Models.Dms;
using AIReception.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIReception.Mvc.Controllers;

[ApiController]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentsService _appointmentsService;

    public AppointmentsController(AppointmentsService appointmentsService)
    {
        _appointmentsService = appointmentsService;
    }

    [HttpGet("/api/appointments")]
    public IActionResult GetAppointments()
    {
        return Ok(new { appointments = _appointmentsService.GetAppointments() });
    }

    [HttpPost("/api/appointments")]
    public IActionResult CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        return Ok(_appointmentsService.CreateAppointment(request));
    }

    [HttpPost("/api/appointments/book")]
    public IActionResult BookAppointment([FromBody] CreateAppointmentRequest request)
    {
        return Ok(_appointmentsService.CreateAppointment(request));
    }

    [HttpGet("/api/appointments/slots")]
    public IActionResult GetAvailableSlots([FromQuery] string? date)
    {
        return Ok(_appointmentsService.GetAvailableSlots(date));
    }
}
