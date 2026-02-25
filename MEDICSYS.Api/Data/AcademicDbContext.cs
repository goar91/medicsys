using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Models.Academico;

namespace MEDICSYS.Api.Data;

public class AcademicDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AcademicDbContext(DbContextOptions<AcademicDbContext> options) : base(options) { }

    public DbSet<AcademicAppointment> AcademicAppointments => Set<AcademicAppointment>();
    public DbSet<AcademicClinicalHistory> AcademicClinicalHistories => Set<AcademicClinicalHistory>();
    public DbSet<AcademicReviewCommentTemplate> AcademicReviewCommentTemplates => Set<AcademicReviewCommentTemplate>();
    public DbSet<AcademicReminder> AcademicReminders => Set<AcademicReminder>();
    public DbSet<AcademicPatient> AcademicPatients => Set<AcademicPatient>();
    public DbSet<AcademicAccreditationCriterion> AcademicAccreditationCriteria => Set<AcademicAccreditationCriterion>();
    public DbSet<AcademicAccreditationEvidence> AcademicAccreditationEvidences => Set<AcademicAccreditationEvidence>();
    public DbSet<AcademicImprovementPlanAction> AcademicImprovementPlanActions => Set<AcademicImprovementPlanAction>();
    public DbSet<AcademicDataConsent> AcademicDataConsents => Set<AcademicDataConsent>();
    public DbSet<AcademicDataAnonymizationRequest> AcademicDataAnonymizationRequests => Set<AcademicDataAnonymizationRequest>();
    public DbSet<AcademicDataRetentionPolicy> AcademicDataRetentionPolicies => Set<AcademicDataRetentionPolicy>();
    public DbSet<AcademicDataAuditEvent> AcademicDataAuditEvents => Set<AcademicDataAuditEvent>();
    public DbSet<AcademicIntegrationConnector> AcademicIntegrationConnectors => Set<AcademicIntegrationConnector>();
    public DbSet<AcademicIntegrationSyncLog> AcademicIntegrationSyncLogs => Set<AcademicIntegrationSyncLog>();
    public DbSet<AcademicStudentRiskFlag> AcademicStudentRiskFlags => Set<AcademicStudentRiskFlag>();
    public DbSet<AcademicSupervisionAssignment> AcademicSupervisionAssignments => Set<AcademicSupervisionAssignment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // AcademicAppointment
        builder.Entity<AcademicAppointment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PatientName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            entity.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Professor)
                .WithMany()
                .HasForeignKey(e => e.ProfessorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.StartAt);
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.ProfessorId);
        });

        // AcademicClinicalHistory
        builder.Entity<AcademicClinicalHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Data).HasColumnType("jsonb");
            entity.Property(e => e.ProfessorComments).HasMaxLength(2000);
            entity.Property(e => e.Grade).HasPrecision(5, 2);
            entity.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ReviewedByProfessor)
                .WithMany()
                .HasForeignKey(e => e.ReviewedByProfessorId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SubmittedAt);
        });

        // AcademicReviewCommentTemplate
        builder.Entity<AcademicReviewCommentTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(120);
            entity.Property(e => e.CommentText).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Category).HasMaxLength(120);
            entity.HasOne(e => e.Professor)
                .WithMany()
                .HasForeignKey(e => e.ProfessorId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.ProfessorId);
            entity.HasIndex(e => new { e.ProfessorId, e.Title }).IsUnique();
        });

        // AcademicReminder
        builder.Entity<AcademicReminder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
            entity.HasOne(e => e.Appointment)
                .WithMany()
                .HasForeignKey(e => e.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => e.Status);
        });

        // AcademicPatient
        builder.Entity<AcademicPatient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IdNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Gender).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.BloodType).HasMaxLength(10);
            entity.Property(e => e.Allergies).HasMaxLength(1000);
            entity.Property(e => e.MedicalConditions).HasMaxLength(1000);
            entity.Property(e => e.EmergencyContact).HasMaxLength(100);
            entity.Property(e => e.EmergencyPhone).HasMaxLength(20);
            entity.HasOne(e => e.CreatedByProfessor)
                .WithMany()
                .HasForeignKey(e => e.CreatedByProfessorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.IdNumber).IsUnique();
            entity.HasIndex(e => e.CreatedByProfessorId);
        });

        // AcademicAccreditationCriterion
        builder.Entity<AcademicAccreditationCriterion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Dimension).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Description).HasMaxLength(1500);
            entity.Property(e => e.TargetValue).HasPrecision(8, 2);
            entity.Property(e => e.CurrentValue).HasPrecision(8, 2);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Dimension);
            entity.HasIndex(e => e.Status);
        });

        // AcademicAccreditationEvidence
        builder.Entity<AcademicAccreditationEvidence>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(220);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.SourceType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EvidenceUrl).HasMaxLength(600);
            entity.HasOne(e => e.Criterion)
                .WithMany(c => c.Evidences)
                .HasForeignKey(e => e.CriterionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.UploadedByUser)
                .WithMany()
                .HasForeignKey(e => e.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.VerifiedByUser)
                .WithMany()
                .HasForeignKey(e => e.VerifiedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.CriterionId);
            entity.HasIndex(e => e.IsVerified);
        });

        // AcademicImprovementPlanAction
        builder.Entity<AcademicImprovementPlanAction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(800);
            entity.Property(e => e.Responsible).IsRequired().HasMaxLength(160);
            entity.Property(e => e.ProgressPercent).HasPrecision(5, 2);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasOne(e => e.Criterion)
                .WithMany(c => c.ImprovementActions)
                .HasForeignKey(e => e.CriterionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.CriterionId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);
        });

        // AcademicDataConsent
        builder.Entity<AcademicDataConsent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SubjectType).IsRequired().HasMaxLength(60);
            entity.Property(e => e.SubjectIdentifier).IsRequired().HasMaxLength(220);
            entity.Property(e => e.Purpose).IsRequired().HasMaxLength(300);
            entity.Property(e => e.LegalBasis).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Notes).HasMaxLength(1500);
            entity.HasOne(e => e.CollectedByUser)
                .WithMany()
                .HasForeignKey(e => e.CollectedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.SubjectType, e.SubjectIdentifier });
            entity.HasIndex(e => e.GrantedAt);
        });

        // AcademicDataAnonymizationRequest
        builder.Entity<AcademicDataAnonymizationRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SubjectType).IsRequired().HasMaxLength(60);
            entity.Property(e => e.SubjectIdentifier).IsRequired().HasMaxLength(220);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.ResolutionNotes).HasMaxLength(1500);
            entity.HasOne(e => e.RequestedByUser)
                .WithMany()
                .HasForeignKey(e => e.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ReviewedByUser)
                .WithMany()
                .HasForeignKey(e => e.ReviewedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.RequestedAt);
        });

        // AcademicDataRetentionPolicy
        builder.Entity<AcademicDataRetentionPolicy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DataCategory).IsRequired().HasMaxLength(120);
            entity.HasOne(e => e.ConfiguredByUser)
                .WithMany()
                .HasForeignKey(e => e.ConfiguredByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.DataCategory).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // AcademicDataAuditEvent
        builder.Entity<AcademicDataAuditEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasConversion<string>();
            entity.Property(e => e.Path).IsRequired().HasMaxLength(320);
            entity.Property(e => e.Method).IsRequired().HasMaxLength(16);
            entity.Property(e => e.UserEmail).HasMaxLength(180);
            entity.Property(e => e.UserRole).HasMaxLength(80);
            entity.Property(e => e.SubjectType).HasMaxLength(80);
            entity.Property(e => e.SubjectIdentifier).HasMaxLength(220);
            entity.Property(e => e.Reason).HasMaxLength(1500);
            entity.Property(e => e.IpAddress).HasMaxLength(80);
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.UserId);
        });

        // AcademicIntegrationConnector
        builder.Entity<AcademicIntegrationConnector>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(140);
            entity.Property(e => e.ProviderType).HasConversion<string>();
            entity.Property(e => e.EndpointUrl).HasMaxLength(600);
            entity.Property(e => e.ApiKeyHint).HasMaxLength(200);
            entity.Property(e => e.LastStatus).HasMaxLength(64);
            entity.Property(e => e.LastMessage).HasMaxLength(1500);
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.ProviderType);
            entity.HasIndex(e => e.Enabled);
        });

        // AcademicIntegrationSyncLog
        builder.Entity<AcademicIntegrationSyncLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Message).HasMaxLength(1500);
            entity.HasOne(e => e.Integration)
                .WithMany(i => i.SyncLogs)
                .HasForeignKey(e => e.IntegrationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.IntegrationId);
            entity.HasIndex(e => e.StartedAt);
        });

        // AcademicStudentRiskFlag
        builder.Entity<AcademicStudentRiskFlag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RiskLevel).HasConversion<string>();
            entity.Property(e => e.Notes).IsRequired().HasMaxLength(1200);
            entity.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ResolvedByUser)
                .WithMany()
                .HasForeignKey(e => e.ResolvedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.RiskLevel);
            entity.HasIndex(e => e.IsResolved);
        });

        // AcademicSupervisionAssignment
        builder.Entity<AcademicSupervisionAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(800);
            entity.HasOne(e => e.Professor)
                .WithMany()
                .HasForeignKey(e => e.ProfessorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.AssignedByUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.ProfessorId);
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.ProfessorId, e.StudentId, e.PatientId });
        });
    }
}
