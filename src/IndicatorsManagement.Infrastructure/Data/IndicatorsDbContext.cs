using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Data;

public class IndicatorsDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public IndicatorsDbContext(DbContextOptions<IndicatorsDbContext> options) : base(options) { }

    public DbSet<Entity> Entities => Set<Entity>();
    public DbSet<Indicator> Indicators => Set<Indicator>();
    public DbSet<ReportingPeriod> ReportingPeriods => Set<ReportingPeriod>();
    public DbSet<Dimension> Dimensions => Set<Dimension>();
    public DbSet<DimensionValue> DimensionValues => Set<DimensionValue>();
    public DbSet<IndicatorAssignment> IndicatorAssignments => Set<IndicatorAssignment>();
    public DbSet<SubmissionObligation> SubmissionObligations => Set<SubmissionObligation>();
    public DbSet<IndicatorEntry> IndicatorEntries => Set<IndicatorEntry>();
    public DbSet<IndicatorEntryDimension> IndicatorEntryDimensions => Set<IndicatorEntryDimension>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<VersionHistory> VersionHistories => Set<VersionHistory>();
    public DbSet<ValidationRule> ValidationRules => Set<ValidationRule>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<DraftRecovery> DraftRecoveries => Set<DraftRecovery>();

    // V2.1 entities
    public DbSet<PublicationHistory> PublicationHistories => Set<PublicationHistory>();
    public DbSet<TargetValue> TargetValues => Set<TargetValue>();
    public DbSet<ReopenRequest> ReopenRequests => Set<ReopenRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureApplicationUser(builder);
        ConfigureEntity(builder);
        ConfigureIndicator(builder);
        ConfigureReportingPeriod(builder);
        ConfigureDimension(builder);
        ConfigureDimensionValue(builder);
        ConfigureIndicatorAssignment(builder);
        ConfigureSubmissionObligation(builder);
        ConfigureIndicatorEntry(builder);
        ConfigureIndicatorEntryDimension(builder);
        ConfigureAttachment(builder);
        ConfigureVersionHistory(builder);
        ConfigureValidationRule(builder);
        ConfigureSystemConfiguration(builder);
        ConfigureAuditLog(builder);
        ConfigureNotification(builder);
        ConfigureUserSession(builder);
        ConfigureDraftRecovery(builder);

        // V2.1 entities
        ConfigurePublicationHistory(builder);
        ConfigureTargetValue(builder);
        ConfigureReopenRequest(builder);
    }

    private static void ConfigureApplicationUser(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FullNameAr).HasMaxLength(255).IsRequired();
            e.Property(u => u.Phone).HasMaxLength(20);
            e.HasOne(u => u.Entity)
                .WithMany()
                .HasForeignKey(u => u.EntityId)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(u => u.EntityId);
            e.HasIndex(u => u.IsActive);
        });
    }

    private static void ConfigureEntity(ModelBuilder builder)
    {
        builder.Entity<Entity>(e =>
        {
            e.ToTable("entities");
            e.Property(x => x.NameAr).HasMaxLength(255).IsRequired();
            e.Property(x => x.NameEn).HasMaxLength(255);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("active");
            e.HasIndex(x => x.NameAr).IsUnique();
            e.HasIndex(x => x.ParentEntityId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Type);
            e.HasOne(x => x.ParentEntity)
                .WithMany(x => x.ChildEntities)
                .HasForeignKey(x => x.ParentEntityId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureIndicator(ModelBuilder builder)
    {
        builder.Entity<Indicator>(e =>
        {
            e.ToTable("indicators");
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(255).IsRequired();
            e.Property(x => x.NameEn).HasMaxLength(255);
            e.Property(x => x.DefinitionAr).IsRequired();
            e.Property(x => x.CalculationMethodAr).IsRequired();
            e.Property(x => x.UnitAr).HasMaxLength(100).IsRequired();
            e.Property(x => x.DataSourceAr).IsRequired();
            e.Property(x => x.PublicationFrequency).HasConversion<string>().HasMaxLength(50);
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.IsActive);
            e.HasIndex(x => x.PublicationFrequency);
            e.HasOne(x => x.Creator)
                .WithMany()
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureReportingPeriod(ModelBuilder builder)
    {
        builder.Entity<ReportingPeriod>(e =>
        {
            e.ToTable("reporting_periods");
            e.Property(x => x.PeriodType).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.DisplayNameAr).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.PeriodType, x.Year, x.Month, x.Quarter, x.HalfYear }).IsUnique();
            e.HasIndex(x => new { x.PeriodType, x.Year });
            e.HasIndex(x => new { x.StartDate, x.EndDate });
            e.HasIndex(x => x.IsOpen);
        });
    }

    private static void ConfigureDimension(ModelBuilder builder)
    {
        builder.Entity<Dimension>(e =>
        {
            e.ToTable("dimensions");
            e.Property(x => x.DimensionNameAr).HasMaxLength(100).IsRequired();
            e.Property(x => x.DimensionType).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.IndicatorId, x.DimensionNameAr }).IsUnique();
            e.HasIndex(x => x.IndicatorId);
            e.HasOne(x => x.Indicator)
                .WithMany(x => x.Dimensions)
                .HasForeignKey(x => x.IndicatorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDimensionValue(ModelBuilder builder)
    {
        builder.Entity<DimensionValue>(e =>
        {
            e.ToTable("dimension_values");
            e.Property(x => x.ValueAr).HasMaxLength(255).IsRequired();
            e.Property(x => x.ValueEn).HasMaxLength(255);
            e.HasOne(x => x.Dimension)
                .WithMany(x => x.Values)
                .HasForeignKey(x => x.DimensionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureIndicatorAssignment(ModelBuilder builder)
    {
        builder.Entity<IndicatorAssignment>(e =>
        {
            e.ToTable("indicator_assignments");
            e.Property(x => x.ReportingFrequency).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.IndicatorId, x.EntityId });
            e.HasOne(x => x.Indicator)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.IndicatorId)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Entity)
                .WithMany(x => x.IndicatorAssignments)
                .HasForeignKey(x => x.EntityId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureSubmissionObligation(ModelBuilder builder)
    {
        builder.Entity<SubmissionObligation>(e =>
        {
            e.ToTable("submission_obligations");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.IndicatorAssignmentId, x.ReportingPeriodId }).IsUnique();
            e.HasOne(x => x.IndicatorAssignment)
                .WithMany(x => x.Obligations)
                .HasForeignKey(x => x.IndicatorAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ReportingPeriod)
                .WithMany()
                .HasForeignKey(x => x.ReportingPeriodId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureIndicatorEntry(ModelBuilder builder)
    {
        builder.Entity<IndicatorEntry>(e =>
        {
            e.ToTable("indicator_entries");
            e.Property(x => x.ValueNumeric).HasPrecision(18, 4);
            e.Property(x => x.WorkflowState).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.PublicationStatus).HasConversion<string>().HasMaxLength(20)
                .HasDefaultValue(PublicationStatus.Unpublished);
            e.Property(x => x.UnitSnapshot).HasMaxLength(100);

            // Filtered unique index: one active entry per indicator/entity/period
            // Excludes soft-deleted and Rejected entries
            e.HasIndex(x => new { x.IndicatorId, x.EntityId, x.ReportingPeriodId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0 AND [WorkflowState] != 'Rejected'");

            e.HasIndex(x => x.IndicatorId);
            e.HasIndex(x => x.EntityId);
            e.HasIndex(x => x.ReportingPeriodId);
            e.HasIndex(x => x.WorkflowState);
            e.HasIndex(x => x.EnteredBy);

            e.HasOne(x => x.Indicator).WithMany().HasForeignKey(x => x.IndicatorId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Entity).WithMany().HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.ReportingPeriod).WithMany().HasForeignKey(x => x.ReportingPeriodId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.EnteredByUser).WithMany().HasForeignKey(x => x.EnteredBy).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedBy).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.EntityApprovedByUser).WithMany().HasForeignKey(x => x.EntityApprovedBy).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.MinistryApprovedByUser).WithMany().HasForeignKey(x => x.MinistryApprovedBy).OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureIndicatorEntryDimension(ModelBuilder builder)
    {
        builder.Entity<IndicatorEntryDimension>(e =>
        {
            e.ToTable("indicator_entry_dimensions");
            e.Property(x => x.ValueNumeric).HasPrecision(18, 4);
            e.HasOne(x => x.IndicatorEntry)
                .WithMany(x => x.EntryDimensions)
                .HasForeignKey(x => x.IndicatorEntryId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Dimension)
                .WithMany()
                .HasForeignKey(x => x.DimensionId)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.DimensionValue)
                .WithMany()
                .HasForeignKey(x => x.DimensionValueId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureAttachment(ModelBuilder builder)
    {
        builder.Entity<Attachment>(e =>
        {
            e.ToTable("attachments");
            e.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            e.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
            e.Property(x => x.FileType).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.IndicatorEntry)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.IndicatorEntryId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.UploadedByUser)
                .WithMany()
                .HasForeignKey(x => x.UploadedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureVersionHistory(ModelBuilder builder)
    {
        builder.Entity<VersionHistory>(e =>
        {
            e.ToTable("version_history");
            e.Property(x => x.ValueNumeric).HasPrecision(18, 4);
            e.Property(x => x.WorkflowState).HasMaxLength(30);
            e.HasOne(x => x.IndicatorEntry)
                .WithMany()
                .HasForeignKey(x => x.IndicatorEntryId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ChangedByUser)
                .WithMany()
                .HasForeignKey(x => x.ChangedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureValidationRule(ModelBuilder builder)
    {
        builder.Entity<ValidationRule>(e =>
        {
            e.ToTable("validation_rules");
            e.Property(x => x.RuleType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.MinValue).HasPrecision(18, 4);
            e.Property(x => x.MaxValue).HasPrecision(18, 4);
            e.HasOne(x => x.Indicator)
                .WithMany(x => x.ValidationRules)
                .HasForeignKey(x => x.IndicatorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureSystemConfiguration(ModelBuilder builder)
    {
        builder.Entity<SystemConfiguration>(e =>
        {
            e.ToTable("system_configuration");
            e.Property(x => x.ConfigKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.ConfigValue).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.ConfigKey).IsUnique();
            e.HasOne(x => x.UpdatedByUser)
                .WithMany()
                .HasForeignKey(x => x.UpdatedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureAuditLog(ModelBuilder builder)
    {
        builder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.ActionType).HasMaxLength(50).IsRequired();
            e.Property(x => x.ResultStatus).HasMaxLength(20).HasDefaultValue("Success");
            e.Property(x => x.IpAddress).HasMaxLength(45);
            // S10 — SHA-256 hex is 64 chars.
            e.Property(x => x.PreviousHash).HasMaxLength(64).IsRequired().HasDefaultValue("");
            e.Property(x => x.RowHash).HasMaxLength(64).IsRequired().HasDefaultValue("");
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.ActionType);
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureNotification(ModelBuilder builder)
    {
        builder.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.Property(x => x.NotificationType).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.TitleAr).HasMaxLength(255).IsRequired();
            e.Property(x => x.MessageAr).IsRequired();
            e.Property(x => x.RelatedEntityType).HasMaxLength(100);
            e.HasIndex(x => new { x.UserId, x.IsRead });
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureUserSession(ModelBuilder builder)
    {
        builder.Entity<UserSession>(e =>
        {
            e.ToTable("user_sessions");
            e.Property(x => x.SessionToken).HasMaxLength(2000).IsRequired();
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.Property(x => x.UserAgent).HasMaxLength(500);
            e.HasIndex(x => x.SessionToken);
            e.HasIndex(x => x.ExpiresAt);
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureDraftRecovery(ModelBuilder builder)
    {
        builder.Entity<DraftRecovery>(e =>
        {
            e.ToTable("draft_recovery");
            e.HasIndex(x => new { x.UserId, x.IndicatorId, x.EntityId, x.ReportingPeriodId });
            e.HasIndex(x => x.ExpiresAt);
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // --- V2.1 Configurations ---

    private static void ConfigurePublicationHistory(ModelBuilder builder)
    {
        builder.Entity<PublicationHistory>(e =>
        {
            e.ToTable("publication_history");
            e.Property(x => x.Action).HasMaxLength(20).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.HasIndex(x => x.IndicatorEntryId);
            e.HasIndex(x => x.PerformedAt);
            e.HasOne(x => x.IndicatorEntry).WithMany().HasForeignKey(x => x.IndicatorEntryId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.PerformedByUser).WithMany().HasForeignKey(x => x.PerformedBy).OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureTargetValue(ModelBuilder builder)
    {
        builder.Entity<TargetValue>(e =>
        {
            e.ToTable("target_values");
            e.Property(x => x.Value).HasPrecision(18, 4);
            e.HasIndex(x => new { x.IndicatorId, x.Year, x.EntityId, x.DimensionValueId }).IsUnique();
            e.HasOne(x => x.Indicator).WithMany().HasForeignKey(x => x.IndicatorId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Entity).WithMany().HasForeignKey(x => x.EntityId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.DimensionValue).WithMany().HasForeignKey(x => x.DimensionValueId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureReopenRequest(ModelBuilder builder)
    {
        builder.Entity<ReopenRequest>(e =>
        {
            e.ToTable("reopen_requests");
            e.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.Property(x => x.ReviewNotes).HasMaxLength(1000);
            e.HasIndex(x => x.IndicatorEntryId);
            e.HasIndex(x => x.Status);
            e.HasOne(x => x.IndicatorEntry).WithMany().HasForeignKey(x => x.IndicatorEntryId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedBy).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedBy).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
