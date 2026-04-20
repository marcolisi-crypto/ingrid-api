using AIReception.Mvc.Data;
using AIReception.Mvc.Data.Entities;
using AIReception.Mvc.Models.Dms;
using Microsoft.EntityFrameworkCore;

namespace AIReception.Mvc.Services;

public class AppointmentsService
{
    private readonly IDbContextFactory<IngridDmsDbContext> _dbContextFactory;
    private readonly DmsCoreService _dmsCore;

    public AppointmentsService(IDbContextFactory<IngridDmsDbContext> dbContextFactory, DmsCoreService dmsCore)
    {
        _dbContextFactory = dbContextFactory;
        _dmsCore = dmsCore;
    }

    public IReadOnlyList<AppointmentRecord> GetAppointments()
    {
        using var db = _dbContextFactory.CreateDbContext();
        return db.Appointments.OrderByDescending(x => x.CreatedAtUtc).AsEnumerable().Select(MapAppointment).ToList();
    }

    public AppointmentRecord CreateAppointment(CreateAppointmentRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var appointment = new DmsAppointmentEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            VehicleId = request.VehicleId,
            FirstName = (request.FirstName ?? "").Trim(),
            LastName = (request.LastName ?? "").Trim(),
            Phone = NormalizePhone(request.Phone),
            Email = (request.Email ?? "").Trim(),
            Make = (request.Make ?? "").Trim(),
            Model = (request.Model ?? "").Trim(),
            Year = (request.Year ?? "").Trim(),
            Vin = (request.Vin ?? "").Trim().ToUpperInvariant(),
            Service = (request.Service ?? "").Trim(),
            Advisor = (request.Advisor ?? "").Trim(),
            Date = (request.Date ?? "").Trim(),
            Time = (request.Time ?? "").Trim(),
            Transport = (request.Transport ?? "").Trim(),
            Notes = (request.Notes ?? "").Trim(),
            Status = "scheduled",
            ScheduledStartUtc = ParseScheduledStart(request.Date, request.Time),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.Appointments.Add(appointment);
        db.SaveChanges();

        _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
        {
            CustomerId = appointment.CustomerId,
            VehicleId = appointment.VehicleId,
            EventType = "appointment.created",
            Title = "Appointment scheduled",
            Body = $"{appointment.Service} {appointment.Date} {appointment.Time}".Trim(),
            SourceSystem = "ingrid.appointments"
        });

        return MapAppointment(appointment);
    }

    public AppointmentRecord? UpdateAppointment(Guid appointmentId, UpdateAppointmentRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var appointment = db.Appointments.FirstOrDefault(x => x.Id == appointmentId);
        if (appointment == null) return null;

        appointment.Advisor = string.IsNullOrWhiteSpace(request.Advisor) ? appointment.Advisor : request.Advisor.Trim();
        appointment.Status = string.IsNullOrWhiteSpace(request.Status) ? appointment.Status : request.Status.Trim();
        appointment.Transport = string.IsNullOrWhiteSpace(request.Transport) ? appointment.Transport : request.Transport.Trim();
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            appointment.Notes = string.IsNullOrWhiteSpace(appointment.Notes)
                ? request.Notes.Trim()
                : $"{appointment.Notes}\n{request.Notes.Trim()}";
        }
        appointment.UpdatedAtUtc = DateTime.UtcNow;
        db.SaveChanges();

        _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
        {
            CustomerId = appointment.CustomerId,
            VehicleId = appointment.VehicleId,
            EventType = "appointment.updated",
            Title = "Appointment updated",
            Body = $"{appointment.Service} • Advisor {appointment.Advisor} • {appointment.Status}".Trim(),
            Department = "service",
            SourceSystem = "ingrid.appointments",
            SourceId = appointment.Id.ToString()
        });

        return MapAppointment(appointment);
    }

    public object GetAvailableSlots(string? date)
    {
        var day = string.IsNullOrWhiteSpace(date) ? DateTime.UtcNow.Date : DateTime.Parse(date).Date;
        var slots = new[] { "08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00", "11:30", "13:00", "13:30", "14:00", "14:30", "15:00", "15:30" }
            .Select(time => new { time, available = true })
            .ToArray();

        return new
        {
            date = day.ToString("yyyy-MM-dd"),
            slots
        };
    }

    private static AppointmentRecord MapAppointment(DmsAppointmentEntity appointment)
    {
        return new AppointmentRecord
        {
            Id = appointment.Id,
            CustomerId = appointment.CustomerId,
            VehicleId = appointment.VehicleId,
            FirstName = appointment.FirstName,
            LastName = appointment.LastName,
            Phone = appointment.Phone,
            Email = appointment.Email,
            Make = appointment.Make,
            Model = appointment.Model,
            Year = appointment.Year,
            Vin = appointment.Vin,
            Service = appointment.Service,
            Advisor = appointment.Advisor,
            Date = appointment.Date,
            Time = appointment.Time,
            Transport = appointment.Transport,
            Notes = appointment.Notes,
            Status = appointment.Status,
            ScheduledStartUtc = appointment.ScheduledStartUtc,
            CreatedAtUtc = appointment.CreatedAtUtc,
            UpdatedAtUtc = appointment.UpdatedAtUtc
        };
    }

    private static DateTime? ParseScheduledStart(string? date, string? time)
    {
        if (DateTime.TryParse($"{date} {time}", out var parsed))
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Local).ToUniversalTime();
        }

        return null;
    }

    private static string NormalizePhone(string? raw)
    {
        var digits = new string((raw ?? "").Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits)) return "";
        if (digits.Length == 10) return $"+1{digits}";
        if (digits.Length == 11 && digits.StartsWith("1")) return $"+{digits}";
        return $"+{digits}";
    }
}
