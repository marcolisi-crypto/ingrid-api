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

public class CreateRepairOrderRequest
{
    public Guid? CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string? Advisor { get; set; }
    public string? Complaint { get; set; }
    public int? OdometerIn { get; set; }
    public string? TransportOption { get; set; }
    public string? Notes { get; set; }
    public DateTime? PromiseAtUtc { get; set; }
}

public class UpdateRepairOrderStatusRequest
{
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
}

public class CreateRepairOrderEstimateLineRequest
{
    public string? LineType { get; set; }
    public string? OpCode { get; set; }
    public string? Description { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Department { get; set; }
    public string? Status { get; set; }
}

public class CreateRepairOrderPartLineRequest
{
    public string? PartNumber { get; set; }
    public string? Description { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? Status { get; set; }
    public string? Source { get; set; }
}

public class CreateTechnicianClockEventRequest
{
    public string? TechnicianName { get; set; }
    public string? EventType { get; set; }
    public string? LaborOpCode { get; set; }
    public string? Notes { get; set; }
    public DateTime? OccurredAtUtc { get; set; }
}

public class CreateAccountingEntryRequest
{
    public string? EntryType { get; set; }
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public string? Status { get; set; }
}

public class CreateRepairOrderLaborOpRequest
{
    public string? OpCode { get; set; }
    public string? Description { get; set; }
    public string? TechnicianName { get; set; }
    public decimal? SoldHours { get; set; }
    public decimal? FlatRateHours { get; set; }
    public decimal? ActualHours { get; set; }
    public string? DispatchStatus { get; set; }
    public string? PayType { get; set; }
    public DateTime? DispatchedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class CreateMultiPointInspectionRequest
{
    public string? Category { get; set; }
    public string? ItemName { get; set; }
    public string? Result { get; set; }
    public string? Severity { get; set; }
    public string? Notes { get; set; }
    public string? TechnicianName { get; set; }
    public DateTime? InspectedAtUtc { get; set; }
}

public class CreateWarrantyClaimRequest
{
    public string? ClaimNumber { get; set; }
    public string? ClaimType { get; set; }
    public string? OpCode { get; set; }
    public string? FailureCode { get; set; }
    public string? Cause { get; set; }
    public string? Correction { get; set; }
    public decimal? ClaimAmount { get; set; }
    public string? Status { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
}

public class CreateRepairOrderPaySplitRequest
{
    public string? PayType { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Percentage { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}

public class CreatePartInventoryItemRequest
{
    public string? PartNumber { get; set; }
    public string? Description { get; set; }
    public string? Manufacturer { get; set; }
    public string? SourceType { get; set; }
    public string? BinLocation { get; set; }
    public decimal? QuantityOnHand { get; set; }
    public decimal? QuantityReserved { get; set; }
    public decimal? QuantityOnOrder { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? ListPrice { get; set; }
    public string? PricingMatrixCode { get; set; }
    public string? Status { get; set; }
    public bool? IsObsolete { get; set; }
}

public class CreatePartOrderRequest
{
    public Guid? RepairOrderId { get; set; }
    public Guid? InventoryItemId { get; set; }
    public string? PartNumber { get; set; }
    public string? Vendor { get; set; }
    public string? OrderType { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Status { get; set; }
    public bool? IsSpecialOrder { get; set; }
    public DateTime? EtaAtUtc { get; set; }
}

public class CreatePartReturnRequest
{
    public Guid? RepairOrderId { get; set; }
    public Guid? InventoryItemId { get; set; }
    public string? PartNumber { get; set; }
    public decimal? Quantity { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }
    public bool? IsObsolescence { get; set; }
}

public class CreateGlAccountRequest
{
    public string? AccountNumber { get; set; }
    public string? Description { get; set; }
    public string? AccountType { get; set; }
    public string? Department { get; set; }
    public string? OemStatementGroup { get; set; }
    public bool? IsActive { get; set; }
}

public class CreateGlEntryRequest
{
    public Guid? RepairOrderId { get; set; }
    public Guid GlAccountId { get; set; }
    public string? JournalCode { get; set; }
    public string? Description { get; set; }
    public decimal? DebitAmount { get; set; }
    public decimal? CreditAmount { get; set; }
    public DateTime? PostedAtUtc { get; set; }
}

public class CreateAccountsPayableBillRequest
{
    public Guid? RepairOrderId { get; set; }
    public string? VendorName { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal? Amount { get; set; }
    public string? Status { get; set; }
    public DateTime? DueAtUtc { get; set; }
}

public class CreateAccountsReceivableInvoiceRequest
{
    public Guid? RepairOrderId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal? Amount { get; set; }
    public decimal? BalanceDue { get; set; }
    public string? Status { get; set; }
    public DateTime? DueAtUtc { get; set; }
}

public class CreateBankReconciliationRequest
{
    public string? AccountNumber { get; set; }
    public DateTime? StatementEndingAtUtc { get; set; }
    public decimal? StatementBalance { get; set; }
    public decimal? BookBalance { get; set; }
    public string? Status { get; set; }
}

public class CreateAccountingClosePeriodRequest
{
    public string? PeriodName { get; set; }
    public DateTime? PeriodStartUtc { get; set; }
    public DateTime? PeriodEndUtc { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}

public class RepairOrderEstimateLineRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepairOrderId { get; set; }
    public string LineType { get; set; } = "labor";
    public string OpCode { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
    public string Department { get; set; } = "service";
    public string Status { get; set; } = "open";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class RepairOrderPartLineRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepairOrderId { get; set; }
    public string PartNumber { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
    public string Status { get; set; } = "requested";
    public string Source { get; set; } = "stock";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class TechnicianClockEventRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepairOrderId { get; set; }
    public string TechnicianName { get; set; } = "";
    public string EventType { get; set; } = "clock_in";
    public string LaborOpCode { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AccountingEntryRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepairOrderId { get; set; }
    public string EntryType { get; set; } = "estimate";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "open";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class RepairOrderLaborOpRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepairOrderId { get; set; }
    public string OpCode { get; set; } = "";
    public string Description { get; set; } = "";
    public string TechnicianName { get; set; } = "";
    public decimal SoldHours { get; set; }
    public decimal FlatRateHours { get; set; }
    public decimal ActualHours { get; set; }
    public string DispatchStatus { get; set; } = "queued";
    public string PayType { get; set; } = "customer";
    public DateTime? DispatchedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class MultiPointInspectionRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepairOrderId { get; set; }
    public string Category { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string Result { get; set; } = "green";
    public string Severity { get; set; } = "normal";
    public string Notes { get; set; } = "";
    public string TechnicianName { get; set; } = "";
    public DateTime InspectedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class WarrantyClaimRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepairOrderId { get; set; }
    public string ClaimNumber { get; set; } = "";
    public string ClaimType { get; set; } = "warranty";
    public string OpCode { get; set; } = "";
    public string FailureCode { get; set; } = "";
    public string Cause { get; set; } = "";
    public string Correction { get; set; } = "";
    public decimal ClaimAmount { get; set; }
    public string Status { get; set; } = "draft";
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class RepairOrderPaySplitRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RepairOrderId { get; set; }
    public string PayType { get; set; } = "customer";
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public string Status { get; set; } = "open";
    public string Notes { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class PartInventoryItemRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PartNumber { get; set; } = "";
    public string Description { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string SourceType { get; set; } = "oem";
    public string BinLocation { get; set; } = "";
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityOnOrder { get; set; }
    public decimal UnitCost { get; set; }
    public decimal ListPrice { get; set; }
    public string PricingMatrixCode { get; set; } = "";
    public string Status { get; set; } = "active";
    public bool IsObsolete { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class PartOrderRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? RepairOrderId { get; set; }
    public Guid? InventoryItemId { get; set; }
    public string PartNumber { get; set; } = "";
    public string Vendor { get; set; } = "";
    public string OrderType { get; set; } = "stock";
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitCost { get; set; }
    public string Status { get; set; } = "ordered";
    public bool IsSpecialOrder { get; set; }
    public DateTime? EtaAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class PartReturnRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? RepairOrderId { get; set; }
    public Guid? InventoryItemId { get; set; }
    public string PartNumber { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "requested";
    public bool IsObsolescence { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class GlAccountRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AccountNumber { get; set; } = "";
    public string Description { get; set; } = "";
    public string AccountType { get; set; } = "asset";
    public string Department { get; set; } = "";
    public string OemStatementGroup { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class GlEntryRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? RepairOrderId { get; set; }
    public Guid GlAccountId { get; set; }
    public string JournalCode { get; set; } = "GEN";
    public string Description { get; set; } = "";
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public DateTime PostedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AccountsPayableBillRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? RepairOrderId { get; set; }
    public string VendorName { get; set; } = "";
    public string InvoiceNumber { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "open";
    public DateTime? DueAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AccountsReceivableInvoiceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? RepairOrderId { get; set; }
    public Guid? CustomerId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal BalanceDue { get; set; }
    public string Status { get; set; } = "open";
    public DateTime? DueAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class BankReconciliationRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AccountNumber { get; set; } = "";
    public DateTime StatementEndingAtUtc { get; set; } = DateTime.UtcNow;
    public decimal StatementBalance { get; set; }
    public decimal BookBalance { get; set; }
    public string Status { get; set; } = "open";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AccountingClosePeriodRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string PeriodName { get; set; } = "";
    public DateTime PeriodStartUtc { get; set; } = DateTime.UtcNow;
    public DateTime PeriodEndUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "open";
    public string Notes { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class RepairOrderRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
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
    public decimal CustomerPaySubtotal { get; set; }
    public decimal WarrantyPaySubtotal { get; set; }
    public decimal InternalPaySubtotal { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public IReadOnlyList<RepairOrderEstimateLineRecord> EstimateLines { get; set; } = Array.Empty<RepairOrderEstimateLineRecord>();
    public IReadOnlyList<RepairOrderPartLineRecord> PartLines { get; set; } = Array.Empty<RepairOrderPartLineRecord>();
    public IReadOnlyList<TechnicianClockEventRecord> TechnicianClockEvents { get; set; } = Array.Empty<TechnicianClockEventRecord>();
    public IReadOnlyList<AccountingEntryRecord> AccountingEntries { get; set; } = Array.Empty<AccountingEntryRecord>();
    public IReadOnlyList<RepairOrderLaborOpRecord> LaborOps { get; set; } = Array.Empty<RepairOrderLaborOpRecord>();
    public IReadOnlyList<MultiPointInspectionRecord> MultiPointInspections { get; set; } = Array.Empty<MultiPointInspectionRecord>();
    public IReadOnlyList<WarrantyClaimRecord> WarrantyClaims { get; set; } = Array.Empty<WarrantyClaimRecord>();
    public IReadOnlyList<RepairOrderPaySplitRecord> PaySplits { get; set; } = Array.Empty<RepairOrderPaySplitRecord>();
}
