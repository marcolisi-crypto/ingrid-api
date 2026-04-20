using AIReception.Mvc.Data;
using AIReception.Mvc.Data.Entities;
using AIReception.Mvc.Models.Dms;
using Microsoft.EntityFrameworkCore;

namespace AIReception.Mvc.Services;

public class TasksService
{
    private readonly IDbContextFactory<IngridDmsDbContext> _dbContextFactory;
    private readonly DmsCoreService _dmsCore;

    public TasksService(IDbContextFactory<IngridDmsDbContext> dbContextFactory, DmsCoreService dmsCore)
    {
        _dbContextFactory = dbContextFactory;
        _dmsCore = dmsCore;
    }

    public IReadOnlyList<TaskRecord> GetTasks()
    {
        using var db = _dbContextFactory.CreateDbContext();
        return db.Tasks.OrderByDescending(x => x.UpdatedAtUtc).AsEnumerable().Select(MapTask).ToList();
    }

    public TaskRecord CreateTask(CreateTaskRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var task = new DmsTaskEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            VehicleId = request.VehicleId,
            AssignedDepartment = string.IsNullOrWhiteSpace(request.AssignedDepartment) ? string.Empty : request.AssignedDepartment.Trim(),
            AssignedUser = string.IsNullOrWhiteSpace(request.AssignedUser) ? string.Empty : request.AssignedUser.Trim(),
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Follow up" : request.Title.Trim(),
            Description = (request.Description ?? "").Trim(),
            Status = "open",
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "normal" : request.Priority.Trim(),
            DueAtUtc = request.DueAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.Tasks.Add(task);
        db.SaveChanges();

        _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
        {
            CustomerId = task.CustomerId,
            VehicleId = task.VehicleId,
            EventType = "task.created",
            Title = task.Title,
            Body = BuildTaskTimelineBody(task),
            SourceSystem = "ingrid.tasks"
        });

        return MapTask(task);
    }

    public TaskRecord? UpdateTask(Guid taskId, UpdateTaskStatusRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var task = db.Tasks.FirstOrDefault(x => x.Id == taskId);
        if (task == null) return null;

        task.Status = string.IsNullOrWhiteSpace(request.Status) ? task.Status : request.Status.Trim();
        task.AssignedDepartment = string.IsNullOrWhiteSpace(request.AssignedDepartment) ? task.AssignedDepartment : request.AssignedDepartment.Trim();
        task.AssignedUser = string.IsNullOrWhiteSpace(request.AssignedUser) ? task.AssignedUser : request.AssignedUser.Trim();
        task.UpdatedAtUtc = DateTime.UtcNow;
        db.SaveChanges();

        _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
        {
            CustomerId = task.CustomerId,
            VehicleId = task.VehicleId,
            EventType = "task.updated",
            Title = $"Task {task.Status}",
            Body = BuildTaskTimelineBody(task),
            SourceSystem = "ingrid.tasks"
        });

        return MapTask(task);
    }

    private static string BuildTaskTimelineBody(DmsTaskEntity task)
    {
        var assignment = string.IsNullOrWhiteSpace(task.AssignedDepartment) && string.IsNullOrWhiteSpace(task.AssignedUser)
            ? string.Empty
            : $"Assigned to {task.AssignedDepartment}{(string.IsNullOrWhiteSpace(task.AssignedUser) ? string.Empty : $" • {task.AssignedUser}")}";
        return string.Join(" • ", new[] { task.Description, assignment }.Where(item => !string.IsNullOrWhiteSpace(item)));
    }

    private static TaskRecord MapTask(DmsTaskEntity task)
    {
        return new TaskRecord
        {
            Id = task.Id,
            CustomerId = task.CustomerId,
            VehicleId = task.VehicleId,
            AssignedDepartment = task.AssignedDepartment,
            AssignedUser = task.AssignedUser,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            DueAtUtc = task.DueAtUtc,
            CreatedAtUtc = task.CreatedAtUtc,
            UpdatedAtUtc = task.UpdatedAtUtc
        };
    }
}
