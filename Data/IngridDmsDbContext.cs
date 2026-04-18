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
    }
}
