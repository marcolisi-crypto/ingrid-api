using AIReception.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIReception.Mvc.Controllers;

[ApiController]
[Route("api/dms/infrastructure")]
public class DmsInfrastructureController : ControllerBase
{
    private readonly DmsDatabaseStatusService _databaseStatus;

    public DmsInfrastructureController(DmsDatabaseStatusService databaseStatus)
    {
        _databaseStatus = databaseStatus;
    }

    [HttpGet("database-status")]
    public IActionResult GetDatabaseStatus()
    {
        return Ok(_databaseStatus.GetStatus());
    }
}
