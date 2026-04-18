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

        _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
        {
            CustomerId = note.CustomerId,
            VehicleId = note.VehicleId,
            EventType = "note.created",
            Title = "Internal note added",
            Body = note.Body,
            SourceSystem = "ingrid.notes"
        });

        return MapNote(note);
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
