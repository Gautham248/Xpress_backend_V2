using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Data
{
    public class ApiDbContext : DbContext
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options) { }

        // DbSet properties for all tables
        public DbSet<TravelRequest> TravelRequests { get; set; }
        public DbSet<TicketOption> TicketOptions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<RMT> RMTs { get; set; }
        public DbSet<TravelMode> TravelModes { get; set; }
        public DbSet<Airline> Airlines { get; set; }
        public DbSet<RequestStatus> RequestStatuses { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AadharDoc> AadharDocs { get; set; }
        public DbSet<PassportDoc> PassportDocs { get; set; }
        public DbSet<VisaDoc> VisaDocs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure UTC conversion for all DateTime properties
            var utcConverter = new ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            // Apply UTC converter to all DateTime properties
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        modelBuilder.Entity(entityType.Name).Property(property.Name).HasConversion(utcConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        modelBuilder.Entity(entityType.Name).Property(property.Name).HasConversion(nullableUtcConverter);
                    }
                }
            }

            // Configure primary keys
            modelBuilder.Entity<TravelRequest>().HasKey(tr => tr.RequestId);
            modelBuilder.Entity<TicketOption>().HasKey(to => to.OptionId);
            modelBuilder.Entity<User>().HasKey(u => u.UserId);
            modelBuilder.Entity<RMT>().HasKey(r => r.ProjectId);
            modelBuilder.Entity<RMT>().HasIndex(r => r.ProjectCode).IsUnique();
            modelBuilder.Entity<TravelMode>().HasKey(tm => tm.TravelModeId);
            modelBuilder.Entity<Airline>().HasKey(a => a.AirlineId);
            modelBuilder.Entity<RequestStatus>().HasKey(rs => rs.StatusId);
            modelBuilder.Entity<Notification>().HasKey(n => n.NotificationId);
            modelBuilder.Entity<UserNotification>().HasKey(un => un.UserNotificationId);
            modelBuilder.Entity<AuditLog>().HasKey(al => al.LogId);
            modelBuilder.Entity<AadharDoc>().HasKey(ad => ad.AadharId);
            modelBuilder.Entity<PassportDoc>().HasKey(pd => pd.PassportDocId);
            modelBuilder.Entity<VisaDoc>().HasKey(vd => vd.VisaDocId);

            // Configure relationships
            modelBuilder.Entity<TravelRequest>()
                .HasOne(tr => tr.User)
                .WithMany(u => u.TravelRequests)
                .HasForeignKey(tr => tr.UserId);

            modelBuilder.Entity<TravelRequest>()
                .HasOne(tr => tr.SelectedTicketOption)
                .WithMany(to => to.SelectedByTravelRequests)
                .HasForeignKey(tr => tr.SelectedTicketOptionId);

            modelBuilder.Entity<TicketOption>()
                .HasOne(to => to.TravelRequest)
                .WithMany(tr => tr.TicketOptions)
                .HasForeignKey(to => to.RequestId);

            modelBuilder.Entity<TicketOption>()
                .HasOne(to => to.CreatedByUser)
                .WithMany(u => u.CreatedTicketOptions)
                .HasForeignKey(to => to.CreatedByUserId);

            // Fixed relationship: TravelRequest.ProjectCode references RMT.ProjectCode (not ProjectId)
            modelBuilder.Entity<TravelRequest>()
                .HasOne(tr => tr.Project)
                .WithMany(r => r.TravelRequests)
                .HasForeignKey(tr => tr.ProjectCode)
                .HasPrincipalKey(r => r.ProjectCode);

            modelBuilder.Entity<TravelRequest>()
                .HasOne(tr => tr.TravelMode)
                .WithMany(tm => tm.TravelRequests)
                .HasForeignKey(tr => tr.TravelModeId);

            modelBuilder.Entity<TravelRequest>()
                .HasOne(tr => tr.Airline)
                .WithMany(a => a.TravelRequests)
                .HasForeignKey(tr => tr.AirlineId);

            modelBuilder.Entity<TravelRequest>()
                .HasOne(tr => tr.CurrentStatus)
                .WithMany(rs => rs.TravelRequests)
                .HasForeignKey(tr => tr.CurrentStatusId);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.CreatedByUser)
                .WithMany(u => u.CreatedNotifications)
                .HasForeignKey(n => n.CreatedBy);

            modelBuilder.Entity<UserNotification>()
                .HasOne(un => un.User)
                .WithMany(u => u.UserNotifications)
                .HasForeignKey(un => un.UserId);

            modelBuilder.Entity<UserNotification>()
                .HasOne(un => un.Notification)
                .WithMany(n => n.UserNotifications)
                .HasForeignKey(un => un.NotificationId);

            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.TravelRequest)
                .WithMany(tr => tr.AuditLogs)
                .HasForeignKey(al => al.RequestId);

            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(al => al.UserId);

            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.OldStatus)
                .WithMany(rs => rs.OldAuditLogs)
                .HasForeignKey(al => al.OldStatusId);

            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.NewStatus)
                .WithMany(rs => rs.NewAuditLogs)
                .HasForeignKey(al => al.NewStatusId);

            modelBuilder.Entity<AadharDoc>()
                .HasOne(ad => ad.User)
                .WithMany(u => u.AadharDocs)
                .HasForeignKey(ad => ad.UserId);

            modelBuilder.Entity<AadharDoc>()
                .HasOne(ad => ad.CreatedByUser)
                .WithMany(u => u.CreatedAadharDocs)
                .HasForeignKey(ad => ad.CreatedBy);

            modelBuilder.Entity<PassportDoc>()
                .HasOne(pd => pd.User)
                .WithMany(u => u.PassportDocs)
                .HasForeignKey(pd => pd.UserId);

            modelBuilder.Entity<PassportDoc>()
                .HasOne(pd => pd.CreatedByUser)
                .WithMany(u => u.CreatedPassportDocs)
                .HasForeignKey(pd => pd.CreatedBy);

            modelBuilder.Entity<VisaDoc>()
                .HasOne(vd => vd.User)
                .WithMany(u => u.VisaDocs)
                .HasForeignKey(vd => vd.UserId);

            modelBuilder.Entity<VisaDoc>()
                .HasOne(vd => vd.CreatedByUser)
                .WithMany(u => u.CreatedVisaDocs)
                .HasForeignKey(vd => vd.CreatedBy);
        }
    }
}
