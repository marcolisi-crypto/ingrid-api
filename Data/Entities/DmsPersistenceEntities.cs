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

public class DmsMediaAssetEntity
{
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? RepairOrderId { get; set; }
    public Guid? NoteId { get; set; }
    public string ContextType { get; set; } = "vin_archive";
    public string MediaType { get; set; } = "photo";
    public string StorageUrl { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Caption { get; set; } = "";
    public string CapturedBy { get; set; } = "";
    public string Visibility { get; set; } = "internal";
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
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
    public decimal CustomerPaySubtotal { get; set; }
    public decimal WarrantyPaySubtotal { get; set; }
    public decimal InternalPaySubtotal { get; set; }
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

public class DmsRepairOrderLaborOpEntity
{
    public Guid Id { get; set; }
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

public class DmsMultiPointInspectionEntity
{
    public Guid Id { get; set; }
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

public class DmsWarrantyClaimEntity
{
    public Guid Id { get; set; }
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

public class DmsRepairOrderPaySplitEntity
{
    public Guid Id { get; set; }
    public Guid RepairOrderId { get; set; }
    public string PayType { get; set; } = "customer";
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public string Status { get; set; } = "open";
    public string Notes { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DmsPartInventoryItemEntity
{
    public Guid Id { get; set; }
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

public class DmsPartOrderEntity
{
    public Guid Id { get; set; }
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

public class DmsPartReturnEntity
{
    public Guid Id { get; set; }
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

public class DmsGlAccountEntity
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = "";
    public string Description { get; set; } = "";
    public string AccountType { get; set; } = "asset";
    public string Department { get; set; } = "";
    public string OemStatementGroup { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DmsGlEntryEntity
{
    public Guid Id { get; set; }
    public Guid? RepairOrderId { get; set; }
    public Guid GlAccountId { get; set; }
    public string JournalCode { get; set; } = "GEN";
    public string Description { get; set; } = "";
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public DateTime PostedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DmsAccountsPayableBillEntity
{
    public Guid Id { get; set; }
    public Guid? RepairOrderId { get; set; }
    public string VendorName { get; set; } = "";
    public string InvoiceNumber { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "open";
    public DateTime? DueAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DmsAccountsReceivableInvoiceEntity
{
    public Guid Id { get; set; }
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

public class DmsBankReconciliationEntity
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = "";
    public DateTime StatementEndingAtUtc { get; set; } = DateTime.UtcNow;
    public decimal StatementBalance { get; set; }
    public decimal BookBalance { get; set; }
    public string Status { get; set; } = "open";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DmsAccountingClosePeriodEntity
{
    public Guid Id { get; set; }
    public string PeriodName { get; set; } = "";
    public DateTime PeriodStartUtc { get; set; } = DateTime.UtcNow;
    public DateTime PeriodEndUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "open";
    public string Notes { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
