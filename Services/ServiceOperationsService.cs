using AIReception.Mvc.Data;
using AIReception.Mvc.Data.Entities;
using AIReception.Mvc.Models.Dms;
using Microsoft.EntityFrameworkCore;

namespace AIReception.Mvc.Services;

public class ServiceOperationsService
{
    private readonly IDbContextFactory<IngridDmsDbContext> _dbContextFactory;
    private readonly DmsCoreService _dmsCore;

    public ServiceOperationsService(IDbContextFactory<IngridDmsDbContext> dbContextFactory, DmsCoreService dmsCore)
    {
        _dbContextFactory = dbContextFactory;
        _dmsCore = dmsCore;
    }

    public IReadOnlyList<RepairOrderRecord> GetRepairOrders(Guid? customerId = null, Guid? vehicleId = null, string? status = null)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var query = db.RepairOrders.AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        if (vehicleId.HasValue)
        {
            query = query.Where(x => x.VehicleId == vehicleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status.Trim());
        }

        return query
            .OrderByDescending(x => x.OpenedAtUtc)
            .AsEnumerable()
            .Select(entity => MapRepairOrder(db, entity))
            .ToList();
    }

    public RepairOrderRecord? GetRepairOrder(Guid repairOrderId)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var entity = db.RepairOrders.FirstOrDefault(x => x.Id == repairOrderId);
        return entity == null ? null : MapRepairOrder(db, entity);
    }

    public RepairOrderRecord OpenRepairOrder(CreateRepairOrderRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();

        var entity = new DmsRepairOrderEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            VehicleId = request.VehicleId,
            AppointmentId = request.AppointmentId,
            RepairOrderNumber = GenerateRepairOrderNumber(),
            Status = "open",
            Advisor = (request.Advisor ?? "").Trim(),
            Complaint = string.IsNullOrWhiteSpace(request.Complaint) ? "General inspection" : request.Complaint.Trim(),
            OdometerIn = request.OdometerIn,
            TransportOption = (request.TransportOption ?? "").Trim(),
            Notes = (request.Notes ?? "").Trim(),
            PromiseAtUtc = request.PromiseAtUtc,
            OpenedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.RepairOrders.Add(entity);

        if (request.AppointmentId.HasValue)
        {
            var appointment = db.Appointments.FirstOrDefault(x => x.Id == request.AppointmentId.Value);
            if (appointment != null)
            {
                appointment.Status = "checked_in";
                appointment.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        db.SaveChanges();

        _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
        {
            CustomerId = entity.CustomerId,
            VehicleId = entity.VehicleId,
            EventType = "repair_order.opened",
            Title = $"RO opened {entity.RepairOrderNumber}",
            Body = entity.Complaint,
            Department = "service",
            SourceSystem = "ingrid.service",
            SourceId = entity.Id.ToString()
        });

        return MapRepairOrder(db, entity);
    }

    public RepairOrderRecord? UpdateRepairOrderStatus(Guid repairOrderId, UpdateRepairOrderStatusRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var entity = db.RepairOrders.FirstOrDefault(x => x.Id == repairOrderId);
        if (entity == null) return null;

        var nextStatus = string.IsNullOrWhiteSpace(request.Status) ? entity.Status : request.Status.Trim().ToLowerInvariant();
        entity.Status = nextStatus;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            entity.Notes = string.Join("\n", new[] { entity.Notes, request.Notes.Trim() }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        if (nextStatus is "closed" or "completed")
        {
            entity.ClosedAtUtc = request.ClosedAtUtc ?? DateTime.UtcNow;

            if (entity.AppointmentId.HasValue)
            {
                var appointment = db.Appointments.FirstOrDefault(x => x.Id == entity.AppointmentId.Value);
                if (appointment != null)
                {
                    appointment.Status = "completed";
                    appointment.UpdatedAtUtc = DateTime.UtcNow;
                }
            }
        }

        entity.UpdatedAtUtc = DateTime.UtcNow;
        db.SaveChanges();

        _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
        {
            CustomerId = entity.CustomerId,
            VehicleId = entity.VehicleId,
            EventType = nextStatus is "closed" or "completed" ? "repair_order.closed" : "repair_order.updated",
            Title = nextStatus is "closed" or "completed" ? $"RO closed {entity.RepairOrderNumber}" : $"RO {entity.RepairOrderNumber} updated",
            Body = string.IsNullOrWhiteSpace(request.Notes) ? entity.Complaint : request.Notes.Trim(),
            Department = "service",
            SourceSystem = "ingrid.service",
            SourceId = entity.Id.ToString()
        });

        return MapRepairOrder(db, entity);
    }

    public RepairOrderRecord? AddEstimateLine(Guid repairOrderId, CreateRepairOrderEstimateLineRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var entity = db.RepairOrders.FirstOrDefault(x => x.Id == repairOrderId);
        if (entity == null) return null;

        var line = new DmsRepairOrderEstimateLineEntity
        {
            Id = Guid.NewGuid(),
            RepairOrderId = repairOrderId,
            LineType = string.IsNullOrWhiteSpace(request.LineType) ? "labor" : request.LineType.Trim().ToLowerInvariant(),
            OpCode = (request.OpCode ?? "").Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? "Estimate line" : request.Description.Trim(),
            Quantity = request.Quantity.GetValueOrDefault(1m) <= 0 ? 1m : request.Quantity!.Value,
            UnitPrice = request.UnitPrice.GetValueOrDefault(),
            Department = string.IsNullOrWhiteSpace(request.Department) ? "service" : request.Department.Trim().ToLowerInvariant(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "open" : request.Status.Trim().ToLowerInvariant(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.RepairOrderEstimateLines.Add(line);
        RecalculateRepairOrderTotals(db, entity);
        db.SaveChanges();

        _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
        {
            CustomerId = entity.CustomerId,
            VehicleId = entity.VehicleId,
            EventType = "repair_order.estimate_line_added",
            Title = $"Estimate updated for {entity.RepairOrderNumber}",
            Body = $"{line.Description} x{line.Quantity} @ {line.UnitPrice:0.00}",
            Department = "service",
            SourceSystem = "ingrid.service",
            SourceId = line.Id.ToString()
        });

        return MapRepairOrder(db, entity);
    }

    public RepairOrderRecord? AddPartLine(Guid repairOrderId, CreateRepairOrderPartLineRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var entity = db.RepairOrders.FirstOrDefault(x => x.Id == repairOrderId);
        if (entity == null) return null;

        var partLine = new DmsRepairOrderPartLineEntity
        {
            Id = Guid.NewGuid(),
            RepairOrderId = repairOrderId,
            PartNumber = (request.PartNumber ?? "").Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? "Parts line" : request.Description.Trim(),
            Quantity = request.Quantity.GetValueOrDefault(1m) <= 0 ? 1m : request.Quantity!.Value,
            UnitPrice = request.UnitPrice.GetValueOrDefault(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "requested" : request.Status.Trim().ToLowerInvariant(),
            Source = string.IsNullOrWhiteSpace(request.Source) ? "stock" : request.Source.Trim().ToLowerInvariant(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.RepairOrderPartLines.Add(partLine);

        if (entity.Status == "open")
        {
            entity.Status = "parts_pending";
        }

        RecalculateRepairOrderTotals(db, entity);
        db.SaveChanges();

        _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
        {
            CustomerId = entity.CustomerId,
            VehicleId = entity.VehicleId,
            EventType = "repair_order.part_added",
            Title = $"Parts added to {entity.RepairOrderNumber}",
            Body = $"{partLine.PartNumber} {partLine.Description}".Trim(),
            Department = "parts",
            SourceSystem = "ingrid.parts",
            SourceId = partLine.Id.ToString()
        });

        return MapRepairOrder(db, entity);
    }

    public RepairOrderRecord? AddTechnicianClockEvent(Guid repairOrderId, CreateTechnicianClockEventRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var entity = db.RepairOrders.FirstOrDefault(x => x.Id == repairOrderId);
        if (entity == null) return null;

        var clockEvent = new DmsTechnicianClockEventEntity
        {
            Id = Guid.NewGuid(),
            RepairOrderId = repairOrderId,
            TechnicianName = string.IsNullOrWhiteSpace(request.TechnicianName) ? "Unassigned Technician" : request.TechnicianName.Trim(),
            EventType = string.IsNullOrWhiteSpace(request.EventType) ? "clock_in" : request.EventType.Trim().ToLowerInvariant(),
            LaborOpCode = (request.LaborOpCode ?? "").Trim(),
            Notes = (request.Notes ?? "").Trim(),
            OccurredAtUtc = request.OccurredAtUtc ?? DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.TechnicianClockEvents.Add(clockEvent);
        if (clockEvent.EventType == "clock_in")
        {
            entity.Status = "in_progress";
        }
        entity.UpdatedAtUtc = DateTime.UtcNow;
        db.SaveChanges();

        _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
        {
            CustomerId = entity.CustomerId,
            VehicleId = entity.VehicleId,
            EventType = $"technician.{clockEvent.EventType}",
            Title = $"Technician {clockEvent.EventType.Replace("_", " ")}",
            Body = $"{clockEvent.TechnicianName} {clockEvent.LaborOpCode}".Trim(),
            Department = "technicians",
            SourceSystem = "ingrid.technicians",
            SourceId = clockEvent.Id.ToString(),
            OccurredAtUtc = clockEvent.OccurredAtUtc
        });

        return MapRepairOrder(db, entity);
    }

    public RepairOrderRecord? AddAccountingEntry(Guid repairOrderId, CreateAccountingEntryRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var entity = db.RepairOrders.FirstOrDefault(x => x.Id == repairOrderId);
        if (entity == null) return null;

        var accountingEntry = new DmsAccountingEntryEntity
        {
            Id = Guid.NewGuid(),
            RepairOrderId = repairOrderId,
            EntryType = string.IsNullOrWhiteSpace(request.EntryType) ? "payment_request" : request.EntryType.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? "Accounting entry" : request.Description.Trim(),
            Amount = request.Amount.GetValueOrDefault(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "open" : request.Status.Trim().ToLowerInvariant(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.AccountingEntries.Add(accountingEntry);
        RecalculateRepairOrderTotals(db, entity);
        db.SaveChanges();

        _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
        {
            CustomerId = entity.CustomerId,
            VehicleId = entity.VehicleId,
            EventType = "accounting.entry_added",
            Title = $"Accounting updated for {entity.RepairOrderNumber}",
            Body = $"{accountingEntry.EntryType} {accountingEntry.Amount:0.00} {accountingEntry.Description}".Trim(),
            Department = "accounting",
            SourceSystem = "ingrid.accounting",
            SourceId = accountingEntry.Id.ToString()
        });

        return MapRepairOrder(db, entity);
    }

    private static string GenerateRepairOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return $"RO-{timestamp[^8..]}";
    }

    private static void RecalculateRepairOrderTotals(IngridDmsDbContext db, DmsRepairOrderEntity entity)
    {
        var estimateLines = db.RepairOrderEstimateLines.Where(x => x.RepairOrderId == entity.Id).ToList();
        var partLines = db.RepairOrderPartLines.Where(x => x.RepairOrderId == entity.Id).ToList();
        var accountingEntries = db.AccountingEntries.Where(x => x.RepairOrderId == entity.Id).ToList();

        entity.LaborSubtotal = estimateLines
            .Where(x => x.LineType.Contains("labor"))
            .Sum(x => x.Quantity * x.UnitPrice);
        entity.FeesSubtotal = estimateLines
            .Where(x => !x.LineType.Contains("labor"))
            .Sum(x => x.Quantity * x.UnitPrice);
        entity.PartsSubtotal = partLines.Sum(x => x.Quantity * x.UnitPrice);
        entity.PaymentsApplied = accountingEntries
            .Where(x => x.EntryType.Contains("payment") || x.EntryType.Contains("credit"))
            .Sum(x => x.Amount);
        entity.TotalEstimate = entity.LaborSubtotal + entity.PartsSubtotal + entity.FeesSubtotal;
        entity.BalanceDue = entity.TotalEstimate - entity.PaymentsApplied;
        entity.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static RepairOrderRecord MapRepairOrder(IngridDmsDbContext db, DmsRepairOrderEntity entity)
    {
        var estimateLines = db.RepairOrderEstimateLines
            .Where(x => x.RepairOrderId == entity.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(MapEstimateLine)
            .ToList();
        var partLines = db.RepairOrderPartLines
            .Where(x => x.RepairOrderId == entity.Id)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(MapPartLine)
            .ToList();
        var clockEvents = db.TechnicianClockEvents
            .Where(x => x.RepairOrderId == entity.Id)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Select(MapClockEvent)
            .ToList();
        var accountingEntries = db.AccountingEntries
            .Where(x => x.RepairOrderId == entity.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(MapAccountingEntry)
            .ToList();

        return new RepairOrderRecord
        {
            Id = entity.Id,
            CustomerId = entity.CustomerId,
            VehicleId = entity.VehicleId,
            AppointmentId = entity.AppointmentId,
            RepairOrderNumber = entity.RepairOrderNumber,
            Status = entity.Status,
            Advisor = entity.Advisor,
            Complaint = entity.Complaint,
            OdometerIn = entity.OdometerIn,
            TransportOption = entity.TransportOption,
            Notes = entity.Notes,
            PromiseAtUtc = entity.PromiseAtUtc,
            OpenedAtUtc = entity.OpenedAtUtc,
            ClosedAtUtc = entity.ClosedAtUtc,
            LaborSubtotal = entity.LaborSubtotal,
            PartsSubtotal = entity.PartsSubtotal,
            FeesSubtotal = entity.FeesSubtotal,
            PaymentsApplied = entity.PaymentsApplied,
            TotalEstimate = entity.TotalEstimate,
            BalanceDue = entity.BalanceDue,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            EstimateLines = estimateLines,
            PartLines = partLines,
            TechnicianClockEvents = clockEvents,
            AccountingEntries = accountingEntries
        };
    }

    private static RepairOrderEstimateLineRecord MapEstimateLine(DmsRepairOrderEstimateLineEntity entity)
    {
        return new RepairOrderEstimateLineRecord
        {
            Id = entity.Id,
            RepairOrderId = entity.RepairOrderId,
            LineType = entity.LineType,
            OpCode = entity.OpCode,
            Description = entity.Description,
            Quantity = entity.Quantity,
            UnitPrice = entity.UnitPrice,
            Department = entity.Department,
            Status = entity.Status,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static RepairOrderPartLineRecord MapPartLine(DmsRepairOrderPartLineEntity entity)
    {
        return new RepairOrderPartLineRecord
        {
            Id = entity.Id,
            RepairOrderId = entity.RepairOrderId,
            PartNumber = entity.PartNumber,
            Description = entity.Description,
            Quantity = entity.Quantity,
            UnitPrice = entity.UnitPrice,
            Status = entity.Status,
            Source = entity.Source,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static TechnicianClockEventRecord MapClockEvent(DmsTechnicianClockEventEntity entity)
    {
        return new TechnicianClockEventRecord
        {
            Id = entity.Id,
            RepairOrderId = entity.RepairOrderId,
            TechnicianName = entity.TechnicianName,
            EventType = entity.EventType,
            LaborOpCode = entity.LaborOpCode,
            Notes = entity.Notes,
            OccurredAtUtc = entity.OccurredAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }

    private static AccountingEntryRecord MapAccountingEntry(DmsAccountingEntryEntity entity)
    {
        return new AccountingEntryRecord
        {
            Id = entity.Id,
            RepairOrderId = entity.RepairOrderId,
            EntryType = entity.EntryType,
            Description = entity.Description,
            Amount = entity.Amount,
            Status = entity.Status,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }
}
