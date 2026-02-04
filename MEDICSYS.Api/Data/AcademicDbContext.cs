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
    public DbSet<AcademicReminder> AcademicReminders => Set<AcademicReminder>();

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
    }
}
