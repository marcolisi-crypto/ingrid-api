using AIReception.Mvc.Data;
using AIReception.Mvc.Data.Entities;
using AIReception.Mvc.Models.Dms;
using AIReception.Mvc.Models.Sms;
using Microsoft.EntityFrameworkCore;

namespace AIReception.Mvc.Services;

public class DmsCoreService
{
    private readonly IDbContextFactory<IngridDmsDbContext> _dbContextFactory;
    private readonly ILogger<DmsCoreService> _logger;
    private readonly object _seedLock = new();
    private bool _seeded;

    public DmsCoreService(
        IDbContextFactory<IngridDmsDbContext> dbContextFactory,
        ILogger<DmsCoreService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public void Warmup()
    {
        using var db = CreateDbContext();
        _ = db.Dealerships.Count();
    }

    public DmsDashboardRecord GetDashboard()
    {
        using var db = CreateDbContext();

        return new DmsDashboardRecord
        {
            Dealership = MapDealership(db.Dealerships.OrderBy(x => x.CreatedAtUtc).First()),
            CustomerCount = db.Customers.Count(),
            VehicleCount = db.Vehicles.Count(),
            ConversationCount = db.Conversations.Count(),
            TimelineEventCount = db.TimelineEvents.Count(),
            RecentTimeline = db.TimelineEvents
                .OrderByDescending(x => x.OccurredAtUtc)
                .Take(10)
                .AsEnumerable()
                .Select(MapTimelineEvent)
                .ToList()
        };
    }

    public IReadOnlyList<CustomerRecord> GetCustomers()
    {
        using var db = CreateDbContext();
        var customers = db.Customers
            .Include(x => x.Phones)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ToList();

        return customers
            .Select(customer => MapCustomer(customer, db))
            .ToList();
    }

    public CustomerRecord? GetCustomer(Guid customerId)
    {
        using var db = CreateDbContext();
        var customer = db.Customers
            .Include(x => x.Phones)
            .FirstOrDefault(x => x.Id == customerId);

        return customer == null ? null : MapCustomer(customer, db);
    }

    public CustomerRecord CreateCustomer(CreateCustomerRequest request)
    {
        using var db = CreateDbContext();

        var customer = new DmsCustomerEntity
        {
            Id = Guid.NewGuid(),
            FirstName = (request.FirstName ?? "").Trim(),
            LastName = (request.LastName ?? "").Trim(),
            Email = (request.Email ?? "").Trim(),
            PreferredLanguage = (request.PreferredLanguage ?? "").Trim(),
            Status = "active",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        if (string.IsNullOrWhiteSpace(customer.FirstName) && string.IsNullOrWhiteSpace(customer.LastName))
        {
            customer.FirstName = "Customer";
        }

        var normalizedPhone = NormalizePhone(request.PrimaryPhone);
        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            customer.Phones.Add(new DmsCustomerPhoneEntity
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                E164Phone = normalizedPhone,
                PhoneType = "mobile",
                IsPrimary = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        db.Customers.Add(customer);
        db.SaveChanges();

        AddTimelineEvent(db, new TimelineEventRecord
        {
            CustomerId = customer.Id,
            EventType = "customer.created",
            Title = "Customer created",
            Body = string.IsNullOrWhiteSpace(BuildCustomerName(customer.FirstName, customer.LastName))
                ? customer.Email
                : BuildCustomerName(customer.FirstName, customer.LastName),
            SourceSystem = "ingrid.dms",
            OccurredAtUtc = DateTime.UtcNow
        });

        db.SaveChanges();
        return MapCustomer(customer, db);
    }

    public IReadOnlyList<VehicleRecord> GetVehicles()
    {
        using var db = CreateDbContext();
        var vehicles = db.Vehicles
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ToList();

        return vehicles
            .Select(vehicle => MapVehicle(vehicle, db))
            .ToList();
    }

    public VehicleRecord? GetVehicle(Guid vehicleId)
    {
        using var db = CreateDbContext();
        var vehicle = db.Vehicles.FirstOrDefault(x => x.Id == vehicleId);
        return vehicle == null ? null : MapVehicle(vehicle, db);
    }

    public VehicleRecord CreateVehicle(CreateVehicleRequest request)
    {
        using var db = CreateDbContext();
        var normalizedVin = NormalizeVin(request.Vin);

        if (!string.IsNullOrWhiteSpace(normalizedVin))
        {
            var existingVehicle = db.Vehicles.FirstOrDefault(x => x.Vin == normalizedVin);
            if (existingVehicle != null)
            {
                return MapVehicle(existingVehicle, db);
            }
        }

        var vehicle = new DmsVehicleEntity
        {
            Id = Guid.NewGuid(),
            Vin = normalizedVin,
            Year = request.Year,
            Make = (request.Make ?? "").Trim(),
            Model = (request.Model ?? "").Trim(),
            Trim = (request.Trim ?? "").Trim(),
            Mileage = request.Mileage,
            Status = "active",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.Vehicles.Add(vehicle);
        db.SaveChanges();

        if (request.CustomerId.HasValue)
        {
            LinkCustomerVehicleInternal(db, request.CustomerId.Value, vehicle.Id);
        }

        AddTimelineEvent(db, new TimelineEventRecord
        {
            VehicleId = vehicle.Id,
            CustomerId = request.CustomerId,
            EventType = "vehicle.created",
            Title = "Vehicle record created",
            Body = BuildVehicleLabel(vehicle.Year, vehicle.Make, vehicle.Model, vehicle.Trim, vehicle.Vin),
            SourceSystem = "ingrid.dms",
            OccurredAtUtc = DateTime.UtcNow
        });

        db.SaveChanges();
        return MapVehicle(vehicle, db);
    }

    public bool LinkCustomerVehicle(Guid customerId, Guid vehicleId)
    {
        using var db = CreateDbContext();
        var linked = LinkCustomerVehicleInternal(db, customerId, vehicleId);
        if (!linked)
        {
            return false;
        }

        db.SaveChanges();
        return true;
    }

    public IReadOnlyList<TimelineEventRecord> GetTimeline(Guid? customerId = null, Guid? vehicleId = null, int limit = 100)
    {
        using var db = CreateDbContext();
        var query = db.TimelineEvents.AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        if (vehicleId.HasValue)
        {
            query = query.Where(x => x.VehicleId == vehicleId.Value);
        }

        return query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(Math.Clamp(limit, 1, 500))
            .AsEnumerable()
            .Select(MapTimelineEvent)
            .ToList();
    }

    public TimelineEventRecord AddTimelineEvent(CreateTimelineEventRequest request)
    {
        using var db = CreateDbContext();
        var timelineEvent = AddTimelineEvent(db, new TimelineEventRecord
        {
            CustomerId = request.CustomerId,
            VehicleId = request.VehicleId,
            EventType = string.IsNullOrWhiteSpace(request.EventType) ? "note" : request.EventType.Trim(),
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Timeline event" : request.Title.Trim(),
            Body = (request.Body ?? "").Trim(),
            Department = (request.Department ?? "").Trim(),
            SourceSystem = string.IsNullOrWhiteSpace(request.SourceSystem) ? "ingrid.dms" : request.SourceSystem.Trim(),
            SourceId = (request.SourceId ?? "").Trim(),
            OccurredAtUtc = request.OccurredAtUtc ?? DateTime.UtcNow
        });

        db.SaveChanges();
        return timelineEvent;
    }

    public CallRecord RecordCallStatus(RecordCallStatusRequest request)
    {
        using var db = CreateDbContext();
        var callSid = (request.CallSid ?? "").Trim();
        if (string.IsNullOrWhiteSpace(callSid))
        {
            throw new InvalidOperationException("CallSid is required.");
        }

        var fromPhone = NormalizePhone(request.FromPhone);
        var toPhone = NormalizePhone(request.ToPhone);
        var customer = GetOrCreateCustomerByPhone(db, ResolveCustomerPhone(fromPhone, toPhone), null);
        var conversation = GetOrCreateConversation(db, "call", callSid, customer.Id);
        var call = db.Calls.FirstOrDefault(x => x.CallSid == callSid);

        if (call == null)
        {
            call = new DmsCallEntity
            {
                Id = Guid.NewGuid(),
                CallSid = callSid,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Calls.Add(call);
        }

        call.CustomerId = customer.Id;
        call.ConversationId = conversation.Id;
        call.ParentCallSid = (request.ParentCallSid ?? "").Trim();
        call.Direction = (request.Direction ?? "").Trim();
        call.FromPhone = fromPhone;
        call.ToPhone = toPhone;
        call.Status = (request.Status ?? "").Trim();
        call.RecordingUrl = (request.RecordingUrl ?? "").Trim();
        call.RecordingSid = (request.RecordingSid ?? "").Trim();
        call.StartedAtUtc ??= request.StartedAtUtc;
        call.EndedAtUtc = request.EndedAtUtc ?? call.EndedAtUtc;
        call.UpdatedAtUtc = DateTime.UtcNow;

        conversation.CustomerId = customer.Id;
        conversation.LastMessagePreview = $"Call {call.Status}".Trim();
        conversation.LastMessageAtUtc = call.EndedAtUtc ?? call.StartedAtUtc ?? DateTime.UtcNow;
        conversation.UpdatedAtUtc = DateTime.UtcNow;

        AddTimelineEvent(db, new TimelineEventRecord
        {
            CustomerId = customer.Id,
            ConversationId = conversation.Id,
            EventType = "call.status",
            Title = $"Call {call.Status}".Trim(),
            Body = $"{call.Direction} {call.FromPhone} {call.ToPhone}".Trim(),
            SourceSystem = string.IsNullOrWhiteSpace(request.SourceSystem) ? "ingrid.communications" : request.SourceSystem.Trim(),
            SourceId = call.CallSid,
            OccurredAtUtc = call.EndedAtUtc ?? call.StartedAtUtc ?? DateTime.UtcNow
        });

        db.SaveChanges();
        return MapCall(call);
    }

    public CallRecord RecordCallTranscript(RecordCallTranscriptRequest request)
    {
        using var db = CreateDbContext();
        var callSid = (request.CallSid ?? "").Trim();
        var call = db.Calls.FirstOrDefault(x => x.CallSid == callSid);
        if (call == null)
        {
            call = new DmsCallEntity
            {
                Id = Guid.NewGuid(),
                CallSid = callSid,
                Status = string.IsNullOrWhiteSpace(request.TranscriptionStatus) ? "transcribed" : request.TranscriptionStatus.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Calls.Add(call);
        }

        call.Transcript = (request.Transcript ?? "").Trim();
        call.RecordingSid = string.IsNullOrWhiteSpace(request.RecordingSid) ? call.RecordingSid : request.RecordingSid.Trim();
        call.UpdatedAtUtc = DateTime.UtcNow;

        AddTimelineEvent(db, new TimelineEventRecord
        {
            CustomerId = call.CustomerId,
            VehicleId = call.VehicleId,
            ConversationId = call.ConversationId,
            EventType = "call.transcript",
            Title = "Call transcript received",
            Body = call.Transcript,
            SourceSystem = "ingrid.communications",
            SourceId = call.CallSid,
            OccurredAtUtc = DateTime.UtcNow
        });

        db.SaveChanges();
        return MapCall(call);
    }

    public CallRecord? GetCallBySid(string callSid)
    {
        using var db = CreateDbContext();
        var call = db.Calls.FirstOrDefault(x => x.CallSid == callSid);
        return call == null ? null : MapCall(call);
    }

    public IReadOnlyList<CallRecord> GetCalls(int limit = 100)
    {
        using var db = CreateDbContext();
        return db.Calls
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Take(Math.Clamp(limit, 1, 500))
            .ToList()
            .Select(MapCall)
            .ToList();
    }

    public CallRecord SaveCallNotes(string callSid, string notes)
    {
        using var db = CreateDbContext();
        var call = db.Calls.FirstOrDefault(x => x.CallSid == callSid);
        if (call == null)
        {
            call = new DmsCallEntity
            {
                Id = Guid.NewGuid(),
                CallSid = callSid,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Calls.Add(call);
        }

        call.Notes = notes.Trim();
        call.UpdatedAtUtc = DateTime.UtcNow;

        db.Notes.Add(new DmsNoteEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = call.CustomerId,
            VehicleId = call.VehicleId,
            CallSid = call.CallSid,
            Body = call.Notes,
            NoteType = "call",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        AddTimelineEvent(db, new TimelineEventRecord
        {
            CustomerId = call.CustomerId,
            VehicleId = call.VehicleId,
            ConversationId = call.ConversationId,
            EventType = "call.note",
            Title = "Call note saved",
            Body = call.Notes,
            SourceSystem = "ingrid.notes",
            SourceId = call.CallSid,
            OccurredAtUtc = DateTime.UtcNow
        });

        db.SaveChanges();
        return MapCall(call);
    }

    public void RecordSmsMessage(SmsMessageRecord record)
    {
        if (record == null)
        {
            return;
        }

        var externalPhone = ResolveExternalPhone(record);
        if (string.IsNullOrWhiteSpace(externalPhone))
        {
            return;
        }

        using var db = CreateDbContext();
        var customer = GetOrCreateCustomerByPhone(db, externalPhone, record.Language);
        var conversation = GetOrCreateConversation(db, "sms", externalPhone, customer.Id);
        var occurredAtUtc = ParseTimestamp(record.CreatedAt);
        var direction = record.Type.Contains("out", StringComparison.OrdinalIgnoreCase) ? "outbound" : "inbound";

        conversation.CustomerId = customer.Id;
        conversation.LastMessagePreview = record.Body;
        conversation.LastMessageAtUtc = occurredAtUtc;
        conversation.MessageCount += 1;
        conversation.UpdatedAtUtc = DateTime.UtcNow;

        AddTimelineEvent(db, new TimelineEventRecord
        {
            CustomerId = customer.Id,
            ConversationId = conversation.Id,
            EventType = $"sms.{direction}",
            Title = direction == "inbound" ? "Inbound SMS" : "Outbound SMS",
            Body = record.Body,
            Department = record.RoutedDepartment,
            SourceSystem = "ingrid.communications",
            SourceId = record.MessageSid,
            OccurredAtUtc = occurredAtUtc
        });

        db.SaveChanges();
    }

    private IngridDmsDbContext CreateDbContext()
    {
        var db = _dbContextFactory.CreateDbContext();
        EnsureSeeded(db);
        return db;
    }

    private void EnsureSeeded(IngridDmsDbContext db)
    {
        if (_seeded)
        {
            return;
        }

        lock (_seedLock)
        {
            if (_seeded)
            {
                return;
            }

            db.Database.EnsureCreated();

            if (!db.Dealerships.Any())
            {
                db.Dealerships.Add(new DmsDealershipEntity
                {
                    Id = Guid.NewGuid(),
                    Code = "ingrid-demo",
                    Name = "INGRID DMS",
                    Timezone = "America/Toronto",
                    Status = "active",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }

            if (!db.Customers.Any())
            {
                var seededAtUtc = DateTime.UtcNow;

                var customer1 = new DmsCustomerEntity
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Sarah",
                    LastName = "Martel",
                    Email = "sarah.martel@ingriddms.demo",
                    PreferredLanguage = "en-CA",
                    Status = "active",
                    CreatedAtUtc = seededAtUtc.AddDays(-14),
                    UpdatedAtUtc = seededAtUtc.AddHours(-3)
                };

                customer1.Phones.Add(new DmsCustomerPhoneEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer1.Id,
                    E164Phone = "+15145550199",
                    PhoneType = "mobile",
                    IsPrimary = true,
                    CreatedAtUtc = seededAtUtc.AddDays(-14)
                });

                var customer2 = new DmsCustomerEntity
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Michael",
                    LastName = "Nguyen",
                    Email = "michael.nguyen@ingriddms.demo",
                    PreferredLanguage = "en-CA",
                    Status = "active",
                    CreatedAtUtc = seededAtUtc.AddDays(-9),
                    UpdatedAtUtc = seededAtUtc.AddHours(-8)
                };

                customer2.Phones.Add(new DmsCustomerPhoneEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer2.Id,
                    E164Phone = "+15145550288",
                    PhoneType = "mobile",
                    IsPrimary = true,
                    CreatedAtUtc = seededAtUtc.AddDays(-9)
                });

                var customer3 = new DmsCustomerEntity
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Nadia",
                    LastName = "Bouchard",
                    Email = "nadia.bouchard@ingriddms.demo",
                    PreferredLanguage = "fr-CA",
                    Status = "active",
                    CreatedAtUtc = seededAtUtc.AddDays(-4),
                    UpdatedAtUtc = seededAtUtc.AddHours(-1)
                };

                customer3.Phones.Add(new DmsCustomerPhoneEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer3.Id,
                    E164Phone = "+15145550377",
                    PhoneType = "mobile",
                    IsPrimary = true,
                    CreatedAtUtc = seededAtUtc.AddDays(-4)
                });

                var vehicle1 = new DmsVehicleEntity
                {
                    Id = Guid.NewGuid(),
                    Vin = "2HNYD2H26CH000001",
                    Year = 2024,
                    Make = "BMW",
                    Model = "X5",
                    Trim = "xDrive40i",
                    Mileage = 12450,
                    Status = "active",
                    CreatedAtUtc = seededAtUtc.AddDays(-14),
                    UpdatedAtUtc = seededAtUtc.AddHours(-3)
                };

                var vehicle2 = new DmsVehicleEntity
                {
                    Id = Guid.NewGuid(),
                    Vin = "WBA53BJ06RCR00002",
                    Year = 2023,
                    Make = "BMW",
                    Model = "330i",
                    Trim = "xDrive",
                    Mileage = 28720,
                    Status = "active",
                    CreatedAtUtc = seededAtUtc.AddDays(-9),
                    UpdatedAtUtc = seededAtUtc.AddHours(-8)
                };

                var vehicle3 = new DmsVehicleEntity
                {
                    Id = Guid.NewGuid(),
                    Vin = "WMW13DJ08P2T00003",
                    Year = 2022,
                    Make = "MINI",
                    Model = "Cooper S",
                    Trim = "5 Door",
                    Mileage = 19440,
                    Status = "active",
                    CreatedAtUtc = seededAtUtc.AddDays(-4),
                    UpdatedAtUtc = seededAtUtc.AddHours(-1)
                };

                var callConversation1 = new DmsConversationEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer1.Id,
                    VehicleId = vehicle1.Id,
                    Channel = "call",
                    ExternalKey = "call:ca_demo_service_001",
                    Status = "closed",
                    LastMessagePreview = "Service call about brake vibration and recall check.",
                    LastMessageAtUtc = seededAtUtc.AddHours(-3),
                    MessageCount = 1,
                    CreatedAtUtc = seededAtUtc.AddHours(-4),
                    UpdatedAtUtc = seededAtUtc.AddHours(-3)
                };

                var callConversation2 = new DmsConversationEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer2.Id,
                    VehicleId = vehicle2.Id,
                    Channel = "call",
                    ExternalKey = "call:ca_demo_sales_002",
                    Status = "open",
                    LastMessagePreview = "Outbound BDC follow-up after lease-end trade inquiry.",
                    LastMessageAtUtc = seededAtUtc.AddHours(-8),
                    MessageCount = 1,
                    CreatedAtUtc = seededAtUtc.AddHours(-9),
                    UpdatedAtUtc = seededAtUtc.AddHours(-8)
                };

                var smsConversation = new DmsConversationEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer3.Id,
                    VehicleId = vehicle3.Id,
                    Channel = "sms",
                    ExternalKey = "sms:+15145550377",
                    Status = "open",
                    LastMessagePreview = "Confirmed tomorrow's maintenance appointment and shuttle request.",
                    LastMessageAtUtc = seededAtUtc.AddHours(-1),
                    MessageCount = 4,
                    CreatedAtUtc = seededAtUtc.AddDays(-1),
                    UpdatedAtUtc = seededAtUtc.AddHours(-1)
                };

                var call1 = new DmsCallEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer1.Id,
                    VehicleId = vehicle1.Id,
                    ConversationId = callConversation1.Id,
                    CallSid = "CA_DEMO_SERVICE_001",
                    Direction = "inbound",
                    FromPhone = "+15145550199",
                    ToPhone = "+14504979243",
                    Status = "completed",
                    Transcript = "Customer reports a front brake vibration at highway speed and asks whether an open recall can be handled during the next service visit.",
                    DetectedLanguage = "en-CA",
                    DetectedDepartment = "service",
                    Notes = "Advise customer that inspection and recall validation will be included at check-in.",
                    StartedAtUtc = seededAtUtc.AddHours(-3).AddMinutes(-8),
                    EndedAtUtc = seededAtUtc.AddHours(-3),
                    CreatedAtUtc = seededAtUtc.AddHours(-3).AddMinutes(-8),
                    UpdatedAtUtc = seededAtUtc.AddHours(-3)
                };

                var call2 = new DmsCallEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer2.Id,
                    VehicleId = vehicle2.Id,
                    ConversationId = callConversation2.Id,
                    CallSid = "CA_DEMO_SALES_002",
                    Direction = "outbound",
                    FromPhone = "+14504979243",
                    ToPhone = "+15145550288",
                    Status = "completed",
                    Transcript = "BDC followed up on a lease maturity list. Customer is interested in seeing 2025 X3 options and asked for a payment comparison.",
                    DetectedLanguage = "en-CA",
                    DetectedDepartment = "sales",
                    Notes = "Create quote comparison and assign sales appointment for Saturday morning.",
                    StartedAtUtc = seededAtUtc.AddHours(-8).AddMinutes(-11),
                    EndedAtUtc = seededAtUtc.AddHours(-8),
                    CreatedAtUtc = seededAtUtc.AddHours(-8).AddMinutes(-11),
                    UpdatedAtUtc = seededAtUtc.AddHours(-8)
                };

                var note1 = new DmsNoteEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer1.Id,
                    VehicleId = vehicle1.Id,
                    CallSid = call1.CallSid,
                    Body = "Customer prefers text confirmation once recall parts availability is confirmed.",
                    NoteType = "service",
                    CreatedAtUtc = seededAtUtc.AddHours(-2),
                    UpdatedAtUtc = seededAtUtc.AddHours(-2)
                };

                var note2 = new DmsNoteEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer2.Id,
                    VehicleId = vehicle2.Id,
                    CallSid = call2.CallSid,
                    Body = "Offer a loyalty payment comparison against current 330i lease plus X3 inventory ETA.",
                    NoteType = "sales",
                    CreatedAtUtc = seededAtUtc.AddHours(-7),
                    UpdatedAtUtc = seededAtUtc.AddHours(-7)
                };

                var task1 = new DmsTaskEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer1.Id,
                    VehicleId = vehicle1.Id,
                    Title = "Confirm brake recall eligibility",
                    Description = "Check campaign eligibility and parts ETA before Thursday service visit.",
                    Status = "open",
                    Priority = "high",
                    DueAtUtc = seededAtUtc.AddDays(1),
                    CreatedAtUtc = seededAtUtc.AddHours(-2),
                    UpdatedAtUtc = seededAtUtc.AddHours(-2)
                };

                var task2 = new DmsTaskEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer2.Id,
                    VehicleId = vehicle2.Id,
                    Title = "Prepare X3 payment comparison",
                    Description = "Build QuickBooks-style proposal sheet with lease and finance side-by-side figures.",
                    Status = "open",
                    Priority = "normal",
                    DueAtUtc = seededAtUtc.AddDays(2),
                    CreatedAtUtc = seededAtUtc.AddHours(-7),
                    UpdatedAtUtc = seededAtUtc.AddHours(-7)
                };

                var task3 = new DmsTaskEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer3.Id,
                    VehicleId = vehicle3.Id,
                    Title = "Arrange shuttle pickup",
                    Description = "Morning shuttle requested for maintenance appointment at 08:30.",
                    Status = "in_progress",
                    Priority = "normal",
                    DueAtUtc = seededAtUtc.AddHours(18),
                    CreatedAtUtc = seededAtUtc.AddHours(-1),
                    UpdatedAtUtc = seededAtUtc.AddMinutes(-45)
                };

                var appointment1 = new DmsAppointmentEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer1.Id,
                    VehicleId = vehicle1.Id,
                    FirstName = customer1.FirstName,
                    LastName = customer1.LastName,
                    Phone = "+15145550199",
                    Email = customer1.Email,
                    Make = vehicle1.Make,
                    Model = vehicle1.Model,
                    Year = vehicle1.Year?.ToString() ?? "",
                    Vin = vehicle1.Vin,
                    Service = "Brake inspection + recall check",
                    Advisor = "Alex Service",
                    Date = seededAtUtc.AddDays(1).ToString("yyyy-MM-dd"),
                    Time = "09:00",
                    Transport = "waiter",
                    Notes = "Customer will wait if repair is under 90 minutes.",
                    Status = "scheduled",
                    ScheduledStartUtc = seededAtUtc.Date.AddDays(1).AddHours(13),
                    CreatedAtUtc = seededAtUtc.AddHours(-2),
                    UpdatedAtUtc = seededAtUtc.AddHours(-2)
                };

                var appointment2 = new DmsAppointmentEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer2.Id,
                    VehicleId = vehicle2.Id,
                    FirstName = customer2.FirstName,
                    LastName = customer2.LastName,
                    Phone = "+15145550288",
                    Email = customer2.Email,
                    Make = vehicle2.Make,
                    Model = vehicle2.Model,
                    Year = vehicle2.Year?.ToString() ?? "",
                    Vin = vehicle2.Vin,
                    Service = "Sales consultation",
                    Advisor = "Jade Sales",
                    Date = seededAtUtc.AddDays(2).ToString("yyyy-MM-dd"),
                    Time = "10:30",
                    Transport = "showroom",
                    Notes = "Review X3 and 5 Series payment options.",
                    Status = "scheduled",
                    ScheduledStartUtc = seededAtUtc.Date.AddDays(2).AddHours(14).AddMinutes(30),
                    CreatedAtUtc = seededAtUtc.AddHours(-7),
                    UpdatedAtUtc = seededAtUtc.AddHours(-7)
                };

                var appointment3 = new DmsAppointmentEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer3.Id,
                    VehicleId = vehicle3.Id,
                    FirstName = customer3.FirstName,
                    LastName = customer3.LastName,
                    Phone = "+15145550377",
                    Email = customer3.Email,
                    Make = vehicle3.Make,
                    Model = vehicle3.Model,
                    Year = vehicle3.Year?.ToString() ?? "",
                    Vin = vehicle3.Vin,
                    Service = "Maintenance service",
                    Advisor = "Luc Service",
                    Date = seededAtUtc.AddDays(1).ToString("yyyy-MM-dd"),
                    Time = "08:30",
                    Transport = "shuttle",
                    Notes = "French-speaking customer requested SMS updates.",
                    Status = "confirmed",
                    ScheduledStartUtc = seededAtUtc.Date.AddDays(1).AddHours(12).AddMinutes(30),
                    CreatedAtUtc = seededAtUtc.AddHours(-1),
                    UpdatedAtUtc = seededAtUtc.AddMinutes(-40)
                };

                db.Customers.AddRange(customer1, customer2, customer3);
                db.Vehicles.AddRange(vehicle1, vehicle2, vehicle3);
                db.Conversations.AddRange(callConversation1, callConversation2, smsConversation);
                db.CustomerVehicles.Add(new DmsCustomerVehicleEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer1.Id,
                    VehicleId = vehicle1.Id,
                    RelationshipType = "owner",
                    IsPrimary = true,
                    CreatedAtUtc = seededAtUtc.AddDays(-14)
                });
                db.CustomerVehicles.Add(new DmsCustomerVehicleEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer2.Id,
                    VehicleId = vehicle2.Id,
                    RelationshipType = "owner",
                    IsPrimary = true,
                    CreatedAtUtc = seededAtUtc.AddDays(-9)
                });
                db.CustomerVehicles.Add(new DmsCustomerVehicleEntity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer3.Id,
                    VehicleId = vehicle3.Id,
                    RelationshipType = "owner",
                    IsPrimary = true,
                    CreatedAtUtc = seededAtUtc.AddDays(-4)
                });
                db.Calls.AddRange(call1, call2);
                db.Notes.AddRange(note1, note2);
                db.Tasks.AddRange(task1, task2, task3);
                db.Appointments.AddRange(appointment1, appointment2, appointment3);
                db.TimelineEvents.AddRange(
                    new DmsTimelineEventEntity
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer1.Id,
                        VehicleId = vehicle1.Id,
                        ConversationId = callConversation1.Id,
                        EventType = "call.received",
                        Title = "Inbound service call received",
                        Body = call1.Transcript,
                        Department = "service",
                        SourceSystem = "ingrid.communications",
                        SourceId = call1.CallSid,
                        OccurredAtUtc = call1.EndedAtUtc ?? seededAtUtc.AddHours(-3),
                        CreatedAtUtc = seededAtUtc.AddHours(-3)
                    },
                    new DmsTimelineEventEntity
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer1.Id,
                        VehicleId = vehicle1.Id,
                        EventType = "task.created",
                        Title = task1.Title,
                        Body = task1.Description,
                        Department = "service",
                        SourceSystem = "ingrid.tasks",
                        SourceId = task1.Id.ToString(),
                        OccurredAtUtc = seededAtUtc.AddHours(-2),
                        CreatedAtUtc = seededAtUtc.AddHours(-2)
                    },
                    new DmsTimelineEventEntity
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer1.Id,
                        VehicleId = vehicle1.Id,
                        EventType = "appointment.created",
                        Title = "Service appointment scheduled",
                        Body = $"{appointment1.Service} on {appointment1.Date} at {appointment1.Time}",
                        Department = "service",
                        SourceSystem = "ingrid.appointments",
                        SourceId = appointment1.Id.ToString(),
                        OccurredAtUtc = appointment1.ScheduledStartUtc ?? seededAtUtc.AddDays(1),
                        CreatedAtUtc = seededAtUtc.AddHours(-2)
                    },
                    new DmsTimelineEventEntity
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer2.Id,
                        VehicleId = vehicle2.Id,
                        ConversationId = callConversation2.Id,
                        EventType = "call.outbound",
                        Title = "Outbound BDC follow-up completed",
                        Body = call2.Transcript,
                        Department = "sales",
                        SourceSystem = "ingrid.communications",
                        SourceId = call2.CallSid,
                        OccurredAtUtc = call2.EndedAtUtc ?? seededAtUtc.AddHours(-8),
                        CreatedAtUtc = seededAtUtc.AddHours(-8)
                    },
                    new DmsTimelineEventEntity
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer2.Id,
                        VehicleId = vehicle2.Id,
                        EventType = "task.created",
                        Title = task2.Title,
                        Body = task2.Description,
                        Department = "sales",
                        SourceSystem = "ingrid.tasks",
                        SourceId = task2.Id.ToString(),
                        OccurredAtUtc = seededAtUtc.AddHours(-7),
                        CreatedAtUtc = seededAtUtc.AddHours(-7)
                    },
                    new DmsTimelineEventEntity
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer2.Id,
                        VehicleId = vehicle2.Id,
                        EventType = "appointment.created",
                        Title = "Sales consultation booked",
                        Body = $"{appointment2.Service} on {appointment2.Date} at {appointment2.Time}",
                        Department = "sales",
                        SourceSystem = "ingrid.appointments",
                        SourceId = appointment2.Id.ToString(),
                        OccurredAtUtc = appointment2.ScheduledStartUtc ?? seededAtUtc.AddDays(2),
                        CreatedAtUtc = seededAtUtc.AddHours(-7)
                    },
                    new DmsTimelineEventEntity
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer3.Id,
                        VehicleId = vehicle3.Id,
                        ConversationId = smsConversation.Id,
                        EventType = "sms.inbound",
                        Title = "Customer confirmed appointment by SMS",
                        Body = "Confirmed tomorrow morning. Please arrange the shuttle pickup.",
                        Department = "service",
                        SourceSystem = "ingrid.communications",
                        SourceId = "SM_DEMO_SMS_003",
                        OccurredAtUtc = seededAtUtc.AddHours(-1),
                        CreatedAtUtc = seededAtUtc.AddHours(-1)
                    },
                    new DmsTimelineEventEntity
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer3.Id,
                        VehicleId = vehicle3.Id,
                        EventType = "task.updated",
                        Title = "Shuttle arrangement in progress",
                        Body = task3.Description,
                        Department = "service",
                        SourceSystem = "ingrid.tasks",
                        SourceId = task3.Id.ToString(),
                        OccurredAtUtc = seededAtUtc.AddMinutes(-45),
                        CreatedAtUtc = seededAtUtc.AddMinutes(-45)
                    },
                    new DmsTimelineEventEntity
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer3.Id,
                        VehicleId = vehicle3.Id,
                        EventType = "appointment.created",
                        Title = "Maintenance appointment confirmed",
                        Body = $"{appointment3.Service} on {appointment3.Date} at {appointment3.Time}",
                        Department = "service",
                        SourceSystem = "ingrid.appointments",
                        SourceId = appointment3.Id.ToString(),
                        OccurredAtUtc = appointment3.ScheduledStartUtc ?? seededAtUtc.AddDays(1),
                        CreatedAtUtc = seededAtUtc.AddMinutes(-40)
                    },
                    new DmsTimelineEventEntity
                    {
                        Id = Guid.NewGuid(),
                        EventType = "dms.seeded",
                        Title = "INGRID demo dataset loaded",
                        Body = "Seeded customers, vehicles, communications, tasks, notes, and appointments for the live demo environment.",
                        SourceSystem = "ingrid.dms",
                        SourceId = "demo-seed-v2",
                        OccurredAtUtc = seededAtUtc,
                        CreatedAtUtc = seededAtUtc
                    });
            }

            db.SaveChanges();
            _seeded = true;
            _logger.LogInformation("INGRID DMS core seed complete using provider {Provider}.", db.Database.ProviderName);
        }
    }

    private bool LinkCustomerVehicleInternal(IngridDmsDbContext db, Guid customerId, Guid vehicleId)
    {
        var customer = db.Customers.FirstOrDefault(x => x.Id == customerId);
        var vehicle = db.Vehicles.FirstOrDefault(x => x.Id == vehicleId);
        if (customer == null || vehicle == null)
        {
            return false;
        }

        var existingLink = db.CustomerVehicles.FirstOrDefault(x => x.CustomerId == customerId && x.VehicleId == vehicleId);
        if (existingLink == null)
        {
            db.CustomerVehicles.Add(new DmsCustomerVehicleEntity
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                VehicleId = vehicleId,
                RelationshipType = "owner",
                IsPrimary = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        customer.UpdatedAtUtc = DateTime.UtcNow;
        vehicle.UpdatedAtUtc = DateTime.UtcNow;

        AddTimelineEvent(db, new TimelineEventRecord
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            EventType = "customer-vehicle.linked",
            Title = "Customer linked to vehicle",
            Body = $"{BuildCustomerName(customer.FirstName, customer.LastName)} linked to {BuildVehicleLabel(vehicle.Year, vehicle.Make, vehicle.Model, vehicle.Trim, vehicle.Vin)}",
            SourceSystem = "ingrid.dms",
            OccurredAtUtc = DateTime.UtcNow
        });

        return true;
    }

    private DmsCustomerEntity GetOrCreateCustomerByPhone(IngridDmsDbContext db, string phone, string? preferredLanguage)
    {
        var normalizedPhone = NormalizePhone(phone);

        var existingCustomer = db.Customers
            .Include(x => x.Phones)
            .FirstOrDefault(x => x.Phones.Any(p => p.E164Phone == normalizedPhone));

        if (existingCustomer != null)
        {
            if (string.IsNullOrWhiteSpace(existingCustomer.PreferredLanguage) && !string.IsNullOrWhiteSpace(preferredLanguage))
            {
                existingCustomer.PreferredLanguage = preferredLanguage.Trim();
                existingCustomer.UpdatedAtUtc = DateTime.UtcNow;
            }

            return existingCustomer;
        }

        var suffix = normalizedPhone.Length >= 4 ? normalizedPhone[^4..] : normalizedPhone;
        var customer = new DmsCustomerEntity
        {
            Id = Guid.NewGuid(),
            FirstName = "Customer",
            LastName = suffix,
            PreferredLanguage = (preferredLanguage ?? "").Trim(),
            Status = "active",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        customer.Phones.Add(new DmsCustomerPhoneEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            E164Phone = normalizedPhone,
            PhoneType = "mobile",
            IsPrimary = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        db.Customers.Add(customer);

        AddTimelineEvent(db, new TimelineEventRecord
        {
            CustomerId = customer.Id,
            EventType = "customer.auto-created",
            Title = "Customer auto-created from communications",
            Body = normalizedPhone,
            SourceSystem = "ingrid.communications",
            OccurredAtUtc = DateTime.UtcNow
        });

        return customer;
    }

    private DmsConversationEntity GetOrCreateConversation(IngridDmsDbContext db, string channel, string externalKey, Guid? customerId)
    {
        var normalizedKey = $"{channel}:{externalKey}".ToLowerInvariant();
        var conversation = db.Conversations.FirstOrDefault(x => x.Channel == channel && x.ExternalKey == normalizedKey);
        if (conversation != null)
        {
            return conversation;
        }

        conversation = new DmsConversationEntity
        {
            Id = Guid.NewGuid(),
            Channel = channel,
            ExternalKey = normalizedKey,
            CustomerId = customerId,
            Status = "open",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.Conversations.Add(conversation);
        return conversation;
    }

    private TimelineEventRecord AddTimelineEvent(IngridDmsDbContext db, TimelineEventRecord timelineEvent)
    {
        db.TimelineEvents.Add(new DmsTimelineEventEntity
        {
            Id = timelineEvent.Id == Guid.Empty ? Guid.NewGuid() : timelineEvent.Id,
            CustomerId = timelineEvent.CustomerId,
            VehicleId = timelineEvent.VehicleId,
            ConversationId = timelineEvent.ConversationId,
            EventType = timelineEvent.EventType,
            Title = timelineEvent.Title,
            Body = timelineEvent.Body,
            Department = timelineEvent.Department,
            SourceSystem = timelineEvent.SourceSystem,
            SourceId = timelineEvent.SourceId,
            OccurredAtUtc = timelineEvent.OccurredAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        });

        return timelineEvent;
    }

    private static CustomerRecord MapCustomer(DmsCustomerEntity customer, IngridDmsDbContext db)
    {
        var vehicleIds = db.CustomerVehicles
            .Where(x => x.CustomerId == customer.Id)
            .Select(x => x.VehicleId)
            .ToList();

        return new CustomerRecord
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            PreferredLanguage = customer.PreferredLanguage,
            Status = customer.Status,
            Phones = customer.Phones.Select(x => x.E164Phone).ToList(),
            VehicleIds = vehicleIds,
            CreatedAtUtc = customer.CreatedAtUtc,
            UpdatedAtUtc = customer.UpdatedAtUtc
        };
    }

    private static VehicleRecord MapVehicle(DmsVehicleEntity vehicle, IngridDmsDbContext db)
    {
        var customerIds = db.CustomerVehicles
            .Where(x => x.VehicleId == vehicle.Id)
            .Select(x => x.CustomerId)
            .ToList();

        return new VehicleRecord
        {
            Id = vehicle.Id,
            Vin = vehicle.Vin,
            Year = vehicle.Year,
            Make = vehicle.Make,
            Model = vehicle.Model,
            Trim = vehicle.Trim,
            Mileage = vehicle.Mileage,
            Status = vehicle.Status,
            CustomerIds = customerIds,
            CreatedAtUtc = vehicle.CreatedAtUtc,
            UpdatedAtUtc = vehicle.UpdatedAtUtc
        };
    }

    private static DealershipRecord MapDealership(DmsDealershipEntity dealership)
    {
        return new DealershipRecord
        {
            Id = dealership.Id,
            Code = dealership.Code,
            Name = dealership.Name,
            Timezone = dealership.Timezone,
            Status = dealership.Status,
            CreatedAtUtc = dealership.CreatedAtUtc,
            UpdatedAtUtc = dealership.UpdatedAtUtc
        };
    }

    private static TimelineEventRecord MapTimelineEvent(DmsTimelineEventEntity entity)
    {
        return new TimelineEventRecord
        {
            Id = entity.Id,
            CustomerId = entity.CustomerId,
            VehicleId = entity.VehicleId,
            ConversationId = entity.ConversationId,
            EventType = entity.EventType,
            Title = entity.Title,
            Body = entity.Body,
            Department = entity.Department,
            SourceSystem = entity.SourceSystem,
            SourceId = entity.SourceId,
            OccurredAtUtc = entity.OccurredAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }

    private static CallRecord MapCall(DmsCallEntity call)
    {
        return new CallRecord
        {
            Id = call.Id,
            CustomerId = call.CustomerId,
            VehicleId = call.VehicleId,
            ConversationId = call.ConversationId,
            CallSid = call.CallSid,
            ParentCallSid = call.ParentCallSid,
            Direction = call.Direction,
            FromPhone = call.FromPhone,
            ToPhone = call.ToPhone,
            Status = call.Status,
            RecordingUrl = call.RecordingUrl,
            RecordingSid = call.RecordingSid,
            Transcript = call.Transcript,
            DetectedLanguage = call.DetectedLanguage,
            DetectedDepartment = call.DetectedDepartment,
            Notes = call.Notes,
            StartedAtUtc = call.StartedAtUtc,
            EndedAtUtc = call.EndedAtUtc,
            CreatedAtUtc = call.CreatedAtUtc,
            UpdatedAtUtc = call.UpdatedAtUtc
        };
    }

    private static string BuildCustomerName(string firstName, string lastName)
    {
        return $"{firstName} {lastName}".Trim();
    }

    private static string BuildVehicleLabel(int? year, string make, string model, string trim, string vin)
    {
        var parts = new[] { year?.ToString(), make, model, trim }
            .Where(x => !string.IsNullOrWhiteSpace(x));

        var label = string.Join(" ", parts);
        return string.IsNullOrWhiteSpace(label) ? vin : label;
    }

    private static string ResolveExternalPhone(SmsMessageRecord record)
    {
        var from = NormalizePhone(record.From);
        var to = NormalizePhone(record.To);
        var twilioVoice = NormalizePhone(Environment.GetEnvironmentVariable("TWILIO_VOICE_NUMBER"));
        var twilioSms = NormalizePhone(Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER"));

        if (!string.IsNullOrWhiteSpace(from) && from != twilioVoice && from != twilioSms)
        {
            return from;
        }

        return to;
    }

    private static string ResolveCustomerPhone(string fromPhone, string toPhone)
    {
        var twilioVoice = NormalizePhone(Environment.GetEnvironmentVariable("TWILIO_VOICE_NUMBER"));
        var twilioSms = NormalizePhone(Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER"));

        if (!string.IsNullOrWhiteSpace(fromPhone) && fromPhone != twilioVoice && fromPhone != twilioSms)
        {
            return fromPhone;
        }

        return toPhone;
    }

    private static string NormalizePhone(string? raw)
    {
        var digits = new string((raw ?? "").Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits))
        {
            return "";
        }

        if (digits.Length == 10)
        {
            return $"+1{digits}";
        }

        if (digits.Length == 11 && digits.StartsWith("1"))
        {
            return $"+{digits}";
        }

        return $"+{digits}";
    }

    private static string NormalizeVin(string? rawVin)
    {
        return (rawVin ?? "").Trim().ToUpperInvariant();
    }

    private static DateTime ParseTimestamp(string? rawTimestamp)
    {
        return DateTime.TryParse(rawTimestamp, out var parsed)
            ? parsed.ToUniversalTime()
            : DateTime.UtcNow;
    }
}
