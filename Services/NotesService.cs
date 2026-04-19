using AIReception.Mvc.Data;
using AIReception.Mvc.Data.Entities;
using AIReception.Mvc.Models.Dms;
using Microsoft.EntityFrameworkCore;

namespace AIReception.Mvc.Services;

public class NotesService
{
    private readonly IDbContextFactory<IngridDmsDbContext> _dbContextFactory;
    private readonly DmsCoreService _dmsCore;

    public NotesService(IDbContextFactory<IngridDmsDbContext> dbContextFactory, DmsCoreService dmsCore)
    {
        _dbContextFactory = dbContextFactory;
        _dmsCore = dmsCore;
    }

    public IReadOnlyList<NoteRecord> GetNotes(Guid? customerId = null, Guid? vehicleId = null, string? callSid = null)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var query = db.Notes.AsQueryable();

        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (vehicleId.HasValue) query = query.Where(x => x.VehicleId == vehicleId.Value);
        if (!string.IsNullOrWhiteSpace(callSid)) query = query.Where(x => x.CallSid == callSid.Trim());

        return query.OrderByDescending(x => x.UpdatedAtUtc)
            .AsEnumerable()
            .Select(MapNote)
            .ToList();
    }

    public NoteRecord CreateNote(CreateNoteRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var note = new DmsNoteEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            VehicleId = request.VehicleId,
            CallSid = (request.CallSid ?? "").Trim(),
            Body = (request.Body ?? "").Trim(),
            NoteType = string.IsNullOrWhiteSpace(request.NoteType) ? "internal" : request.NoteType.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.Notes.Add(note);

        if (!string.IsNullOrWhiteSpace(note.CallSid))
        {
            var call = db.Calls.FirstOrDefault(x => x.CallSid == note.CallSid);
            if (call != null)
            {
                call.Notes = note.Body;
                call.UpdatedAtUtc = DateTime.UtcNow;
                note.CustomerId ??= call.CustomerId;
                note.VehicleId ??= call.VehicleId;
            }
        }

        db.SaveChanges();

        var timelineEvent = InferTimelineEvent(note);
        _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
        {
            CustomerId = note.CustomerId,
            VehicleId = note.VehicleId,
            EventType = timelineEvent.EventType,
            Title = timelineEvent.Title,
            Body = timelineEvent.Body,
            Department = timelineEvent.Department,
            SourceSystem = "ingrid.notes",
            SourceId = note.Id.ToString()
        });

        return MapNote(note);
    }

    private static (string EventType, string Title, string Body, string Department) InferTimelineEvent(DmsNoteEntity note)
    {
        var body = (note.Body ?? string.Empty).Trim();
        var normalized = body.ToLowerInvariant();

        if (normalized.StartsWith("[vehicle]"))
        {
            var cleanedBody = System.Text.RegularExpressions.Regex.Replace(body, @"^\[vehicle\]\s*", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
            var isMovement = cleanedBody.Contains("geo / movement update", StringComparison.OrdinalIgnoreCase)
                || cleanedBody.Contains("current zone", StringComparison.OrdinalIgnoreCase);

            return (
                isMovement ? "vehicle_movement" : "vehicle_health",
                isMovement ? "Vehicle movement" : "Vehicle health",
                cleanedBody,
                "service");
        }

        if (normalized.StartsWith("[archive]"))
        {
            var cleanedBody = System.Text.RegularExpressions.Regex.Replace(body, @"^\[archive\]\s*", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
            return ("vin_archive", "VIN archive", cleanedBody, "service");
        }

        return ("note.created", "Internal note added", body, string.IsNullOrWhiteSpace(note.NoteType) ? "internal" : note.NoteType.Trim());
    }

    private static NoteRecord MapNote(DmsNoteEntity note)
    {
        return new NoteRecord
        {
            Id = note.Id,
            CustomerId = note.CustomerId,
            VehicleId = note.VehicleId,
            CallSid = note.CallSid,
            Body = note.Body,
            NoteType = note.NoteType,
            CreatedAtUtc = note.CreatedAtUtc,
            UpdatedAtUtc = note.UpdatedAtUtc
        };
    }
}
