using AIReception.Mvc.Models.Dms;
using AIReception.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIReception.Mvc.Controllers;

[ApiController]
[Route("api/media")]
public class MediaAssetsController : ControllerBase
{
    private readonly MediaAssetService _mediaAssets;

    public MediaAssetsController(MediaAssetService mediaAssets)
    {
        _mediaAssets = mediaAssets;
    }

    [HttpGet]
    public IActionResult GetMediaAssets(
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? vehicleId,
        [FromQuery] Guid? repairOrderId,
        [FromQuery] string? contextType)
    {
        return Ok(new
        {
            media = _mediaAssets.GetMediaAssets(customerId, vehicleId, repairOrderId, contextType)
        });
    }

    [HttpPost]
    public IActionResult CreateMediaAsset([FromBody] CreateMediaAssetRequest request)
    {
        return Ok(_mediaAssets.CreateMediaAsset(request));
    }
}
