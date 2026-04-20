using AIReception.Mvc.Models.Dms;
using AIReception.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIReception.Mvc.Controllers;

[ApiController]
[Route("api/parts")]
public class PartsManagementController : ControllerBase
{
    private readonly PartsManagementService _partsManagement;

    public PartsManagementController(PartsManagementService partsManagement)
    {
        _partsManagement = partsManagement;
    }

    [HttpGet("inventory")]
    public IActionResult GetInventory([FromQuery] string? status)
    {
        return Ok(new { inventory = _partsManagement.GetInventory(status) });
    }

    [HttpGet("orders")]
    public IActionResult GetPartOrders([FromQuery] Guid? repairOrderId)
    {
        return Ok(new { orders = _partsManagement.GetPartOrders(repairOrderId) });
    }

    [HttpGet("returns")]
    public IActionResult GetPartReturns([FromQuery] Guid? repairOrderId)
    {
        return Ok(new { returns = _partsManagement.GetPartReturns(repairOrderId) });
    }

    [HttpPost("inventory")]
    public IActionResult UpsertInventoryItem([FromBody] CreatePartInventoryItemRequest request)
    {
        return Ok(_partsManagement.UpsertInventoryItem(request));
    }

    [HttpPost("orders")]
    public IActionResult CreatePartOrder([FromBody] CreatePartOrderRequest request)
    {
        return Ok(_partsManagement.CreatePartOrder(request));
    }

    [HttpPost("returns")]
    public IActionResult CreatePartReturn([FromBody] CreatePartReturnRequest request)
    {
        return Ok(_partsManagement.CreatePartReturn(request));
    }
}
