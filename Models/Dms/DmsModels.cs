namespace AIReception.Mvc.Models.Dms;

public class DealershipRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "ingrid-demo";
    public string Name { get; set; } = "INGRID DMS";
    public string Timezone { get; set; } = "America/Toronto";
    public string Status { get; set; } = "active";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class CustomerRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; set; } = "";
    public string PreferredLanguage { get; set; } = "";
    public string Status { get; set; } = "active";
    public List<string> Phones { get; set; } = new();
    public List<Guid> VehicleIds { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class VehicleRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Vin { get; set; } = "";
    public int? Year { get; set; }
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public string Trim { get; set; } = "";
    public int? Mileage { get; set; }
    public string Status { get; set; } = "active";
    public List<Guid> CustomerIds { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ConversationRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public string Channel { get; set; } = "sms";
    public string ExternalKey { get; set; } = "";
    public string Status { get; set; } = "open";
    public string LastMessagePreview { get; set; } = "";
    public DateTime? LastMessageAtUtc { get; set; }
    public int MessageCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class TimelineEventRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? ConversationId { get; set; }
    public string EventType { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Department { get; set; } = "";
    public string SourceSystem { get; set; } = "ingrid";
    public string SourceId { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class CallRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? ConversationId { get; set; }
    public string CallSid { get; set; } = "";
    public string ParentCallSid { get; set; } = "";
    public string Direction { get; set; } = "";
    public string FromPhone { get; set; } = "";
    public string ToPhone { get; set; } = "";
    public string Status { get; set; } = "";
    public string RecordingUrl { get; set; } = "";
    public string RecordingSid { get; set; } = "";
    public string Transcript { get; set; } = "";
    public string DetectedLanguage { get; set; } = "";
    public string DetectedDepartment { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DmsDashboardRecord
{
    public DealershipRecord Dealership { get; set; } = new();
    public int CustomerCount { get; set; }
    public int VehicleCount { get; set; }
    public int ConversationCount { get; set; }
    public int TimelineEventCount { get; set; }
    public IReadOnlyList<TimelineEventRecord> RecentTimeline { get; set; } = Array.Empty<TimelineEventRecord>();
}

public class CreateCustomerRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? PrimaryPhone { get; set; }
}

public class CreateVehicleRequest
{
    public string? Vin { get; set; }
    public int? Year { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? Trim { get; set; }
    public int? Mileage { get; set; }
    public Guid? CustomerId { get; set; }
}

public class LinkCustomerVehicleRequest
{
    public Guid CustomerId { get; set; }
    public Guid VehicleId { get; set; }
}

public class CreateTimelineEventRequest
{
    public Guid? CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public string? EventType { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? Department { get; set; }
    public string? SourceSystem { get; set; }
    public string? SourceId { get; set; }
    public DateTime? OccurredAtUtc { get; set; }
}

public class RecordCallStatusRequest
{
    public string? CallSid { get; set; }
    public string? ParentCallSid { get; set; }
    public string? Direction { get; set; }
    public string? FromPhone { get; set; }
    public string? ToPhone { get; set; }
    public string? Status { get; set; }
    public string? RecordingUrl { get; set; }
    public string? RecordingSid { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public string? SourceSystem { get; set; }
}

public class RecordCallTranscriptRequest
{
    public string? CallSid { get; set; }
    public string? Transcript { get; set; }
    public string? TranscriptionStatus { get; set; }
    public string? RecordingSid { get; set; }
}

public class CreateNoteRequest
{
    public Guid? CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public string? CallSid { get; set; }
    public string? Body { get; set; }
    public string? NoteType { get; set; }
}

public class NoteRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public string CallSid { get; set; } = "";
    public string Body { get; set; } = "";
    public string NoteType { get; set; } = "internal";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class CreateTaskRequest
{
    public Guid? CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public DateTime? DueAtUtc { get; set; }
}

public class UpdateTaskStatusRequest
{
    public string? Status { get; set; }
}

public class TaskRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "open";
    public string Priority { get; set; } = "normal";
    public DateTime? DueAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class CreateAppointmentRequest
{
    public Guid? CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? Year { get; set; }
    public string? Vin { get; set; }
    public string? Service { get; set; }
    public string? Advisor { get; set; }
    public string? Date { get; set; }
    public string? Time { get; set; }
    public string? Transport { get; set; }
    public string? Notes { get; set; }
}

public class AppointmentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public string Year { get; set; } = "";
    public string Vin { get; set; } = "";
    public string Service { get; set; } = "";
    public string Advisor { get; set; } = "";
    public string Date { get; set; } = "";
    public string Time { get; set; } = "";
    public string Transport { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Status { get; set; } = "scheduled";
    public DateTime? ScheduledStartUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
