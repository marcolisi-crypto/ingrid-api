using AIReception.Mvc.Models.Dms;
using AIReception.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIReception.Mvc.Controllers;

[ApiController]
public class TasksController : ControllerBase
{
    private readonly TasksService _tasksService;

    public TasksController(TasksService tasksService)
    {
        _tasksService = tasksService;
    }

    [HttpGet("/api/tasks")]
    public IActionResult GetTasks()
    {
        return Ok(new { tasks = _tasksService.GetTasks() });
    }

    [HttpPost("/api/tasks")]
    public IActionResult CreateTask([FromBody] CreateTaskRequest request)
    {
        return Ok(_tasksService.CreateTask(request));
    }

    [HttpPatch("/api/tasks/{taskId:guid}")]
    public IActionResult UpdateTaskStatus(Guid taskId, [FromBody] UpdateTaskStatusRequest request)
    {
        var task = _tasksService.UpdateTask(taskId, request);
        return task == null ? NotFound(new { error = "Task not found." }) : Ok(task);
    }
}
