using AIReception.Mvc.Models.Dms;
using AIReception.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIReception.Mvc.Controllers;

[ApiController]
[Route("api/service")]
public class ServiceOperationsController : ControllerBase
{
    private readonly ServiceOperationsService _serviceOperations;

    public ServiceOperationsController(ServiceOperationsService serviceOperations)
    {
        _serviceOperations = serviceOperations;
    }

    [HttpGet("repair-orders")]
    public IActionResult GetRepairOrders([FromQuery] Guid? customerId, [FromQuery] Guid? vehicleId, [FromQuery] string? status)
    {
        return Ok(new { repairOrders = _serviceOperations.GetRepairOrders(customerId, vehicleId, status) });
    }

    [HttpGet("repair-orders/{repairOrderId:guid}")]
    public IActionResult GetRepairOrder(Guid repairOrderId)
    {
        var repairOrder = _serviceOperations.GetRepairOrder(repairOrderId);
        return repairOrder == null ? NotFound(new { error = "Repair order not found." }) : Ok(repairOrder);
    }

    [HttpPost("repair-orders/open")]
    public IActionResult OpenRepairOrder([FromBody] CreateRepairOrderRequest request)
    {
        return Ok(_serviceOperations.OpenRepairOrder(request));
    }

    [HttpPost("repair-orders/{repairOrderId:guid}/close")]
    public IActionResult CloseRepairOrder(Guid repairOrderId, [FromBody] UpdateRepairOrderStatusRequest request)
    {
        request.Status ??= "closed";
        var repairOrder = _serviceOperations.UpdateRepairOrderStatus(repairOrderId, request);
        return repairOrder == null ? NotFound(new { error = "Repair order not found." }) : Ok(repairOrder);
    }

    [HttpPatch("repair-orders/{repairOrderId:guid}/status")]
    public IActionResult UpdateRepairOrderStatus(Guid repairOrderId, [FromBody] UpdateRepairOrderStatusRequest request)
    {
        var repairOrder = _serviceOperations.UpdateRepairOrderStatus(repairOrderId, request);
        return repairOrder == null ? NotFound(new { error = "Repair order not found." }) : Ok(repairOrder);
    }

    [HttpPost("repair-orders/{repairOrderId:guid}/estimate-lines")]
    public IActionResult AddEstimateLine(Guid repairOrderId, [FromBody] CreateRepairOrderEstimateLineRequest request)
    {
        var repairOrder = _serviceOperations.AddEstimateLine(repairOrderId, request);
        return repairOrder == null ? NotFound(new { error = "Repair order not found." }) : Ok(repairOrder);
    }

    [HttpPost("repair-orders/{repairOrderId:guid}/parts-lines")]
    public IActionResult AddPartLine(Guid repairOrderId, [FromBody] CreateRepairOrderPartLineRequest request)
    {
        var repairOrder = _serviceOperations.AddPartLine(repairOrderId, request);
        return repairOrder == null ? NotFound(new { error = "Repair order not found." }) : Ok(repairOrder);
    }

    [HttpPost("repair-orders/{repairOrderId:guid}/clock-events")]
    public IActionResult AddTechnicianClockEvent(Guid repairOrderId, [FromBody] CreateTechnicianClockEventRequest request)
    {
        var repairOrder = _serviceOperations.AddTechnicianClockEvent(repairOrderId, request);
        return repairOrder == null ? NotFound(new { error = "Repair order not found." }) : Ok(repairOrder);
    }

    [HttpPost("repair-orders/{repairOrderId:guid}/accounting-entries")]
    public IActionResult AddAccountingEntry(Guid repairOrderId, [FromBody] CreateAccountingEntryRequest request)
    {
        var repairOrder = _serviceOperations.AddAccountingEntry(repairOrderId, request);
        return repairOrder == null ? NotFound(new { error = "Repair order not found." }) : Ok(repairOrder);
    }
}
