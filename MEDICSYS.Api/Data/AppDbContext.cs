using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Models;

namespace MEDICSYS.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ClinicalHistory> ClinicalHistories => Set<ClinicalHistory>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<AccountingCategory> AccountingCategories => Set<AccountingCategory>();
    public DbSet<AccountingEntry> AccountingEntries => Set<AccountingEntry>();
    public DbSet<Patient> Patients => Set<Patient>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ClinicalHistory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Data).HasColumnType("jsonb");
            entity.Property(x => x.Status).HasConversion<string>();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Patient)
                .WithMany(x => x.ClinicalHistories)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReviewedBy)
                .WithMany()
                .HasForeignKey(x => x.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Appointment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>();
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Professor)
                .WithMany()
                .HasForeignKey(x => x.ProfessorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Reminder>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(x => x.Appointment)
                .WithMany()
                .HasForeignKey(x => x.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Invoice>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>();
            entity.Property(x => x.PaymentMethod).HasConversion<string>();
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.DiscountTotal).HasPrecision(18, 2);
            entity.Property(x => x.Tax).HasPrecision(18, 2);
            entity.Property(x => x.Total).HasPrecision(18, 2);
            entity.Property(x => x.CardFeePercent).HasPrecision(5, 2);
            entity.Property(x => x.CardFeeAmount).HasPrecision(18, 2);
            entity.Property(x => x.TotalToCharge).HasPrecision(18, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasMany(x => x.Items)
                .WithOne(x => x.Invoice)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.TaxRate).HasPrecision(5, 2);
            entity.Property(x => x.Tax).HasPrecision(18, 2);
            entity.Property(x => x.Total).HasPrecision(18, 2);
        });

        builder.Entity<AccountingCategory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<string>();
            entity.Property(x => x.MonthlyBudget).HasPrecision(18, 2);
        });

        builder.Entity<AccountingEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<string>();
            entity.Property(x => x.PaymentMethod).HasConversion<string>();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Invoice)
                .WithMany()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Patient>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasIndex(x => x.IdNumber).IsUnique();
            entity.HasOne(x => x.Odontologo)
                .WithMany()
                .HasForeignKey(x => x.OdontologoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.ClinicalHistories)
                .WithOne(x => x.Patient)
                .HasForeignKey(x => x.PatientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
