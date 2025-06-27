
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
        public DbSet<WorkflowTemplate> WorkflowTemplates { get; set; }
        public DbSet<WorkflowStep> WorkflowSteps { get; set; }
        public DbSet<WorkflowRule> WorkflowRules { get; set; }
        public DbSet<WorkflowHistory> WorkflowHistory { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // This part remains unchanged
            #region UTC Conversion and Primary Keys
            // Configure UTC conversion for all DateTime properties
            var utcConverter = new ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

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
            modelBuilder.Entity<TravelMode>().HasKey(tm => tm.TravelModeId);
            modelBuilder.Entity<Airline>().HasKey(a => a.AirlineId);
            modelBuilder.Entity<RequestStatus>().HasKey(rs => rs.StatusId);
            modelBuilder.Entity<Notification>().HasKey(n => n.NotificationId);
            modelBuilder.Entity<UserNotification>().HasKey(un => un.UserNotificationId);
            modelBuilder.Entity<AuditLog>().HasKey(al => al.LogId);
            modelBuilder.Entity<AadharDoc>().HasKey(ad => ad.AadharId);
            modelBuilder.Entity<PassportDoc>().HasKey(pd => pd.PassportDocId);
            modelBuilder.Entity<VisaDoc>().HasKey(vd => vd.VisaDocId);
            #endregion

            // This part remains unchanged
            #region RMT Entity Configuration
            modelBuilder.Entity<RMT>(entity =>
            {
                entity.Property(r => r.ProjectCode).IsRequired().HasMaxLength(50);
                entity.HasIndex(r => r.ProjectCode).IsUnique();
                entity.Property(r => r.ProjectName).IsRequired().HasMaxLength(255);
                entity.Property(r => r.DuId).IsRequired();
                entity.Property(r => r.ProjectStartDate).IsRequired();
                entity.Property(r => r.ProjectEndDate).IsRequired();
                entity.Property(r => r.ProjectManager).HasMaxLength(100);
                entity.Property(r => r.ProjectManagerEmail).HasMaxLength(100);
                entity.Property(r => r.ProjectStatus).IsRequired().HasMaxLength(50);
                entity.Property(r => r.DuHeadName).HasMaxLength(100);
                entity.Property(r => r.DuHeadEmail).HasMaxLength(100);
                entity.Property(r => r.IsActive).HasDefaultValue(true);
            });
            #endregion

            // Configure relationships
            #region Relationships
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

            modelBuilder.Entity<TravelRequest>()
                .HasOne(tr => tr.Project)
                .WithMany(r => r.TravelRequests)
                .HasForeignKey(tr => tr.ProjectCode)
                .HasPrincipalKey(r => r.ProjectCode);

            modelBuilder.Entity<TravelRequest>()
                .HasOne(tr => tr.TravelMode)
                .WithMany(tm => tm.TravelRequests)
                .HasForeignKey(tr => tr.TravelModeId);

            // ***************************************************************
            // ** START: MODIFICATION **

            // STEP 1: REMOVE THE OLD RELATIONSHIP CONFIGURATION
            // modelBuilder.Entity<TravelRequest>()
            //     .HasOne(tr => tr.Airline)
            //     .WithMany(a => a.TravelRequests)
            //     .HasForeignKey(tr => tr.AirlineId);

            // STEP 2: ADD THE NEW 1-TO-MANY RELATIONSHIP CONFIGURATION
            modelBuilder.Entity<TravelRequest>()
                .HasMany(tr => tr.BookedAirlines)      // TravelRequest has many Airlines
                .WithOne(a => a.TravelRequest)         // Each Airline has one TravelRequest
                .HasForeignKey(a => a.RequestId)       // The foreign key is in the Airline table
                .OnDelete(DeleteBehavior.Restrict);    // Prevent deleting a request if it has bookings

            // ** END: MODIFICATION **
            // ***************************************************************

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
            #endregion

            #region WorkflowTemplates Configuration
            modelBuilder.Entity<WorkflowTemplate>(entity =>
            {
                entity.HasKey(wt => wt.WorkflowId);
                entity.Property(wt => wt.WorkflowName).IsRequired().HasMaxLength(100);
                entity.Property(wt => wt.WorkflowDescription).HasMaxLength(500);
                entity.Property(wt => wt.IsActive).HasDefaultValue(true);
                entity.Property(wt => wt.CreatedAt).IsRequired().HasConversion(utcConverter);
                entity.Property(wt => wt.UpdatedAt).IsRequired().HasConversion(utcConverter);
            });
            #endregion

            #region WorkflowSteps Configuration
            modelBuilder.Entity<WorkflowStep>(entity =>
            {
                entity.HasKey(ws => ws.StepId);
                entity.Property(ws => ws.WorkflowId).IsRequired();
                entity.Property(ws => ws.StatusId).IsRequired();
                entity.Property(ws => ws.StepOrder).IsRequired();
                entity.Property(ws => ws.RequiresApproval).HasDefaultValue(false);
                entity.Property(ws => ws.ApproverRole).HasMaxLength(50);
                entity.Property(ws => ws.StepDescription).HasMaxLength(500);
                entity.Property(ws => ws.IsActive).HasDefaultValue(true);
                entity.HasIndex(ws => new { ws.WorkflowId, ws.StepOrder }).IsUnique();
                entity.HasOne(ws => ws.WorkflowTemplate)
                      .WithMany(wt => wt.WorkflowSteps)
                      .HasForeignKey(ws => ws.WorkflowId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ws => ws.RequestStatus)
                      .WithMany(rs => rs.WorkflowSteps)
                      .HasForeignKey(ws => ws.StatusId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            #endregion

            #region WorkflowRules Configuration
            modelBuilder.Entity<WorkflowRule>(entity =>
            {
                entity.HasKey(wr => wr.RuleId);
                entity.Property(wr => wr.WorkflowId).IsRequired();
                entity.Property(wr => wr.UserRole).HasMaxLength(50);
                entity.Property(wr => wr.ProjectCode).HasMaxLength(50);
                entity.Property(wr => wr.Priority).HasDefaultValue(0);
                entity.Property(wr => wr.IsActive).HasDefaultValue(true);
                entity.Property(wr => wr.CreatedAt).IsRequired().HasConversion(utcConverter);
                entity.HasOne(wr => wr.WorkflowTemplate)
                      .WithMany(wt => wt.WorkflowRules)
                      .HasForeignKey(wr => wr.WorkflowId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(wr => wr.RMT)
                      .WithMany(r => r.WorkflowRules)
                      .HasForeignKey(wr => wr.ProjectCode)
                      .HasPrincipalKey(r => r.ProjectCode)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(wr => new { wr.Priority, wr.UserRole, wr.ProjectCode });
            });
            #endregion

            #region WorkflowHistory Configuration
            modelBuilder.Entity<WorkflowHistory>(entity =>
            {
                entity.HasKey(wh => wh.HistoryId);
                entity.Property(wh => wh.RequestId).IsRequired().HasMaxLength(50);
                entity.Property(wh => wh.NewWorkflowId).IsRequired();
                entity.Property(wh => wh.ChangedBy).IsRequired();
                entity.Property(wh => wh.ChangeReason).HasMaxLength(500);
                entity.Property(wh => wh.ChangedAt).IsRequired().HasConversion(utcConverter);
                entity.HasOne(wh => wh.TravelRequest)
                      .WithMany(tr => tr.WorkflowHistories)
                      .HasForeignKey(wh => wh.RequestId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(wh => wh.ChangedByUser)
                      .WithMany(u => u.WorkflowHistories)
                      .HasForeignKey(wh => wh.ChangedBy)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(wh => wh.OldWorkflow)
                      .WithMany(wt => wt.WorkflowHistoriesAsOld)
                      .HasForeignKey(wh => wh.OldWorkflowId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(wh => wh.NewWorkflow)
                      .WithMany(wt => wt.WorkflowHistoriesAsNew)
                      .HasForeignKey(wh => wh.NewWorkflowId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            #endregion

            #region TravelRequest Modifications
            modelBuilder.Entity<TravelRequest>(entity =>
            {
                entity.Property(tr => tr.AssignedWorkflowId).IsRequired(false);
                entity.Property(tr => tr.ReportingManagerEmail).HasMaxLength(100);
                entity.HasOne(tr => tr.WorkflowTemplate)
                      .WithMany(wt => wt.TravelRequests)
                      .HasForeignKey(tr => tr.AssignedWorkflowId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            #endregion
        }
    }
}