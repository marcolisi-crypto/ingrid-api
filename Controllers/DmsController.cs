using AIReception.Mvc.Models.Dms;
using AIReception.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIReception.Mvc.Controllers;

[ApiController]
[Route("api/dms")]
public class DmsController : ControllerBase
{
    private readonly DmsCoreService _dmsCore;

    public DmsController(DmsCoreService dmsCore)
    {
        _dmsCore = dmsCore;
    }

    [HttpGet("dashboard")]
    public IActionResult GetDashboard()
    {
        return Ok(_dmsCore.GetDashboard());
    }

    [HttpGet("customers")]
    public IActionResult GetCustomers()
    {
        return Ok(new { customers = _dmsCore.GetCustomers() });
    }

    [HttpGet("customers/{customerId:guid}")]
    public IActionResult GetCustomer(Guid customerId)
    {
        var customer = _dmsCore.GetCustomer(customerId);
        return customer == null
            ? NotFound(new { error = "Customer not found." })
            : Ok(customer);
    }

    [HttpPost("customers")]
    public IActionResult CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        var customer = _dmsCore.CreateCustomer(request);
        return Ok(customer);
    }

    [HttpGet("vehicles")]
    public IActionResult GetVehicles()
    {
        return Ok(new { vehicles = _dmsCore.GetVehicles() });
    }

    [HttpGet("vehicles/{vehicleId:guid}")]
    public IActionResult GetVehicle(Guid vehicleId)
    {
        var vehicle = _dmsCore.GetVehicle(vehicleId);
        return vehicle == null
            ? NotFound(new { error = "Vehicle not found." })
            : Ok(vehicle);
    }

    [HttpPost("vehicles")]
    public IActionResult CreateVehicle([FromBody] CreateVehicleRequest request)
    {
        var vehicle = _dmsCore.CreateVehicle(request);
        return Ok(vehicle);
    }

    [HttpPost("customer-vehicles")]
    public IActionResult LinkCustomerVehicle([FromBody] LinkCustomerVehicleRequest request)
    {
        var linked = _dmsCore.LinkCustomerVehicle(request.CustomerId, request.VehicleId);
        return linked
            ? Ok(new { success = true })
            : NotFound(new { error = "Customer or vehicle not found." });
    }

    [HttpGet("timeline")]
    public IActionResult GetTimeline([FromQuery] Guid? customerId, [FromQuery] Guid? vehicleId, [FromQuery] int limit = 100)
    {
        return Ok(new
        {
            events = _dmsCore.GetTimeline(customerId, vehicleId, limit)
        });
    }

    [HttpPost("timeline")]
    public IActionResult CreateTimelineEvent([FromBody] CreateTimelineEventRequest request)
    {
        var timelineEvent = _dmsCore.AddTimelineEvent(request);
        return Ok(timelineEvent);
    }
}
