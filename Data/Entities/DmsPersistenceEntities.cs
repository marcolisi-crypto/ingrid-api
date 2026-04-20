namespace AIReception.Mvc.Data.Entities;

public class DmsDealershipEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Timezone { get; set; } = "America/Toronto";
    public string Status { get; set; } = "active";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DmsCustomerEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PreferredLanguage { get; set; } = "";
    public string Status { get; set; } = "active";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<DmsCustomerPhoneEntity> Phones { get; set; } = new();
}

public class DmsCustomerPhoneEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string E164Phone { get; set; } = "";
    public string PhoneType { get; set; } = "mobile";
    public bool IsPrimary { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DmsVehicleEntity
{
    public Guid Id { get; set; }
    public string Vin { get; set; } = "";
    public int? Year { get; set; }
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public string Trim { get; set; } = "";
    public int? Mileage { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DmsCustomerVehicleEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid VehicleId { get; set; }
    public string RelationshipType { get; set; } = "owner";
    public bool IsPrimary { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DmsConversationEntity
{
    public Guid Id { get; set; }
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

public class DmsCallEntity
{
    public Guid Id { get; set; }
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

public class DmsTimelineEventEntity
{
    public Guid Id { get; set; }
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

public class DmsNoteEntity
{
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public string CallSid { get; set; } = "";
    public string Body { get; set; } = "";
    public string NoteType { get; set; } = "internal";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DmsTaskEntity
{
    public Guid Id { get; set; }
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

public class DmsAppointmentEntity
{
    public Guid Id { get; set; }
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

public class DmsRepairOrderEntity
{
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string RepairOrderNumber { get; set; } = "";
    public string Status { get; set; } = "open";
    public string Advisor { get; set; } = "";
    public string Complaint { get; set; } = "";
    public int? OdometerIn { get; set; }
    public string TransportOption { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime? PromiseAtUtc { get; set; }
    public DateTime OpenedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }
    public decimal LaborSubtotal { get; set; }
    public decimal PartsSubtotal { get; set; }
    public decimal FeesSubtotal { get; set; }
    public decimal PaymentsApplied { get; set; }
    public decimal TotalEstimate { get; set; }
    public decimal BalanceDue { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DmsRepairOrderEstimateLineEntity
{
    public Guid Id { get; set; }
    public Guid RepairOrderId { get; set; }
    public string LineType { get; set; } = "labor";
    public string OpCode { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public string Department { get; set; } = "service";
    public string Status { get; set; } = "open";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DmsRepairOrderPartLineEntity
{
    public Guid Id { get; set; }
    public Guid RepairOrderId { get; set; }
    public string PartNumber { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public string Status { get; set; } = "requested";
    public string Source { get; set; } = "stock";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DmsTechnicianClockEventEntity
{
    public Guid Id { get; set; }
    public Guid RepairOrderId { get; set; }
    public string TechnicianName { get; set; } = "";
    public string EventType { get; set; } = "clock_in";
    public string LaborOpCode { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DmsAccountingEntryEntity
{
    public Guid Id { get; set; }
    public Guid RepairOrderId { get; set; }
    public string EntryType { get; set; } = "estimate";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "open";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
