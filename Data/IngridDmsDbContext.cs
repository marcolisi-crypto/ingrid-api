using AIReception.Mvc.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIReception.Mvc.Data;

public class IngridDmsDbContext : DbContext
{
    public IngridDmsDbContext(DbContextOptions<IngridDmsDbContext> options)
        : base(options)
    {
    }

    public DbSet<DmsDealershipEntity> Dealerships => Set<DmsDealershipEntity>();
    public DbSet<DmsCustomerEntity> Customers => Set<DmsCustomerEntity>();
    public DbSet<DmsCustomerPhoneEntity> CustomerPhones => Set<DmsCustomerPhoneEntity>();
    public DbSet<DmsVehicleEntity> Vehicles => Set<DmsVehicleEntity>();
    public DbSet<DmsCustomerVehicleEntity> CustomerVehicles => Set<DmsCustomerVehicleEntity>();
    public DbSet<DmsConversationEntity> Conversations => Set<DmsConversationEntity>();
    public DbSet<DmsCallEntity> Calls => Set<DmsCallEntity>();
    public DbSet<DmsTimelineEventEntity> TimelineEvents => Set<DmsTimelineEventEntity>();
    public DbSet<DmsNoteEntity> Notes => Set<DmsNoteEntity>();
    public DbSet<DmsTaskEntity> Tasks => Set<DmsTaskEntity>();
    public DbSet<DmsAppointmentEntity> Appointments => Set<DmsAppointmentEntity>();
    public DbSet<DmsRepairOrderEntity> RepairOrders => Set<DmsRepairOrderEntity>();
    public DbSet<DmsRepairOrderEstimateLineEntity> RepairOrderEstimateLines => Set<DmsRepairOrderEstimateLineEntity>();
    public DbSet<DmsRepairOrderPartLineEntity> RepairOrderPartLines => Set<DmsRepairOrderPartLineEntity>();
    public DbSet<DmsTechnicianClockEventEntity> TechnicianClockEvents => Set<DmsTechnicianClockEventEntity>();
    public DbSet<DmsAccountingEntryEntity> AccountingEntries => Set<DmsAccountingEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DmsDealershipEntity>(entity =>
        {
            entity.ToTable("dealerships");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(100);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Timezone).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<DmsCustomerEntity>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).HasMaxLength(100);
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.Property(x => x.PreferredLanguage).HasMaxLength(20);
            entity.Property(x => x.Status).HasMaxLength(50);
        });

        modelBuilder.Entity<DmsCustomerPhoneEntity>(entity =>
        {
            entity.ToTable("customer_phones");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.E164Phone).HasMaxLength(25);
            entity.Property(x => x.PhoneType).HasMaxLength(50);
            entity.HasIndex(x => x.E164Phone).IsUnique();
            entity.HasOne<DmsCustomerEntity>()
                .WithMany(x => x.Phones)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DmsVehicleEntity>(entity =>
        {
            entity.ToTable("vehicles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Vin).HasMaxLength(32);
            entity.Property(x => x.Make).HasMaxLength(100);
            entity.Property(x => x.Model).HasMaxLength(100);
            entity.Property(x => x.Trim).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.HasIndex(x => x.Vin);
        });

        modelBuilder.Entity<DmsCustomerVehicleEntity>(entity =>
        {
            entity.ToTable("customer_vehicles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RelationshipType).HasMaxLength(50);
            entity.HasIndex(x => new { x.CustomerId, x.VehicleId }).IsUnique();
        });

        modelBuilder.Entity<DmsConversationEntity>(entity =>
        {
            entity.ToTable("conversations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Channel).HasMaxLength(50);
            entity.Property(x => x.ExternalKey).HasMaxLength(100);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.HasIndex(x => new { x.Channel, x.ExternalKey }).IsUnique();
        });

        modelBuilder.Entity<DmsCallEntity>(entity =>
        {
            entity.ToTable("calls");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CallSid).HasMaxLength(100);
            entity.Property(x => x.ParentCallSid).HasMaxLength(100);
            entity.Property(x => x.Direction).HasMaxLength(50);
            entity.Property(x => x.FromPhone).HasMaxLength(25);
            entity.Property(x => x.ToPhone).HasMaxLength(25);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.Property(x => x.RecordingSid).HasMaxLength(100);
            entity.Property(x => x.DetectedLanguage).HasMaxLength(20);
            entity.Property(x => x.DetectedDepartment).HasMaxLength(50);
            entity.HasIndex(x => x.CallSid).IsUnique();
        });

        modelBuilder.Entity<DmsTimelineEventEntity>(entity =>
        {
            entity.ToTable("timeline_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(100);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.Department).HasMaxLength(50);
            entity.Property(x => x.SourceSystem).HasMaxLength(100);
            entity.Property(x => x.SourceId).HasMaxLength(100);
            entity.HasIndex(x => x.CustomerId);
            entity.HasIndex(x => x.VehicleId);
            entity.HasIndex(x => x.OccurredAtUtc);
        });

        modelBuilder.Entity<DmsNoteEntity>(entity =>
        {
            entity.ToTable("notes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CallSid).HasMaxLength(100);
            entity.Property(x => x.NoteType).HasMaxLength(50);
            entity.HasIndex(x => x.CallSid);
        });

        modelBuilder.Entity<DmsTaskEntity>(entity =>
        {
            entity.ToTable("tasks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.Property(x => x.Priority).HasMaxLength(50);
        });

        modelBuilder.Entity<DmsAppointmentEntity>(entity =>
        {
            entity.ToTable("appointments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Phone).HasMaxLength(25);
            entity.Property(x => x.Email).HasMaxLength(200);
            entity.Property(x => x.Vin).HasMaxLength(32);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.Property(x => x.Service).HasMaxLength(100);
            entity.Property(x => x.Advisor).HasMaxLength(100);
            entity.HasIndex(x => x.ScheduledStartUtc);
        });

        modelBuilder.Entity<DmsRepairOrderEntity>(entity =>
        {
            entity.ToTable("repair_orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RepairOrderNumber).HasMaxLength(50);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.Property(x => x.Advisor).HasMaxLength(100);
            entity.Property(x => x.TransportOption).HasMaxLength(50);
            entity.Property(x => x.LaborSubtotal).HasPrecision(12, 2);
            entity.Property(x => x.PartsSubtotal).HasPrecision(12, 2);
            entity.Property(x => x.FeesSubtotal).HasPrecision(12, 2);
            entity.Property(x => x.PaymentsApplied).HasPrecision(12, 2);
            entity.Property(x => x.TotalEstimate).HasPrecision(12, 2);
            entity.Property(x => x.BalanceDue).HasPrecision(12, 2);
            entity.HasIndex(x => x.RepairOrderNumber).IsUnique();
            entity.HasIndex(x => x.CustomerId);
            entity.HasIndex(x => x.VehicleId);
            entity.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<DmsRepairOrderEstimateLineEntity>(entity =>
        {
            entity.ToTable("repair_order_estimate_lines");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.LineType).HasMaxLength(50);
            entity.Property(x => x.OpCode).HasMaxLength(50);
            entity.Property(x => x.Quantity).HasPrecision(12, 2);
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.Property(x => x.Department).HasMaxLength(50);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.HasIndex(x => x.RepairOrderId);
        });

        modelBuilder.Entity<DmsRepairOrderPartLineEntity>(entity =>
        {
            entity.ToTable("repair_order_part_lines");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PartNumber).HasMaxLength(100);
            entity.Property(x => x.Quantity).HasPrecision(12, 2);
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.Property(x => x.Source).HasMaxLength(50);
            entity.HasIndex(x => x.RepairOrderId);
        });

        modelBuilder.Entity<DmsTechnicianClockEventEntity>(entity =>
        {
            entity.ToTable("technician_clock_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TechnicianName).HasMaxLength(100);
            entity.Property(x => x.EventType).HasMaxLength(50);
            entity.Property(x => x.LaborOpCode).HasMaxLength(50);
            entity.HasIndex(x => x.RepairOrderId);
            entity.HasIndex(x => x.OccurredAtUtc);
        });

        modelBuilder.Entity<DmsAccountingEntryEntity>(entity =>
        {
            entity.ToTable("accounting_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EntryType).HasMaxLength(50);
            entity.Property(x => x.Amount).HasPrecision(12, 2);
            entity.Property(x => x.Status).HasMaxLength(50);
            entity.HasIndex(x => x.RepairOrderId);
        });
    }
}
