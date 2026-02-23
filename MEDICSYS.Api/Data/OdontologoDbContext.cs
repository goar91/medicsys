using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Models;
using MEDICSYS.Api.Models.Odontologia;

namespace MEDICSYS.Api.Data;

public class OdontologoDbContext : DbContext
{
    public OdontologoDbContext(DbContextOptions<OdontologoDbContext> options) : base(options) { }

    public DbSet<OdontologoAppointment> OdontologoAppointments => Set<OdontologoAppointment>();
    public DbSet<OdontologoClinicalHistory> OdontologoClinicalHistories => Set<OdontologoClinicalHistory>();
    public DbSet<OdontologoPatient> OdontologoPatients => Set<OdontologoPatient>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<AccountingEntry> AccountingEntries => Set<AccountingEntry>();
    public DbSet<AccountingCategory> AccountingCategories => Set<AccountingCategory>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryAlert> InventoryAlerts => Set<InventoryAlert>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<InvoiceConfig> InvoiceConfigs => Set<InvoiceConfig>();
    public DbSet<TelemedicineSession> TelemedicineSessions => Set<TelemedicineSession>();
    public DbSet<TelemedicineMessage> TelemedicineMessages => Set<TelemedicineMessage>();
    public DbSet<PatientPortalPreference> PatientPortalPreferences => Set<PatientPortalPreference>();
    public DbSet<PatientPortalNotification> PatientPortalNotifications => Set<PatientPortalNotification>();
    public DbSet<InsuranceClaim> InsuranceClaims => Set<InsuranceClaim>();
    public DbSet<SignedClinicalDocument> SignedClinicalDocuments => Set<SignedClinicalDocument>();
    public DbSet<OdontologoInvoiceOwnership> OdontologoInvoiceOwnerships => Set<OdontologoInvoiceOwnership>();
    public DbSet<OdontologoAccountingEntryOwnership> OdontologoAccountingEntryOwnerships => Set<OdontologoAccountingEntryOwnership>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // OdontologoAppointment
        builder.Entity<OdontologoAppointment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PatientName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(500);
            // No crear relación de FK con usuarios - solo almacenar el ID
            entity.Ignore(e => e.Odontologo);
            entity.HasIndex(e => e.StartAt);
            entity.HasIndex(e => e.OdontologoId);
        });

        // OdontologoClinicalHistory
        builder.Entity<OdontologoClinicalHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PatientName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PatientIdNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Data).HasColumnType("jsonb");
            // No crear relación de FK con usuarios - solo almacenar el ID
            entity.Ignore(e => e.Odontologo);
            entity.HasIndex(e => e.PatientIdNumber);
            entity.HasIndex(e => e.OdontologoId);
        });

        // OdontologoPatient
        builder.Entity<OdontologoPatient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IdNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
            // No crear relación de FK con usuarios - solo almacenar el ID
            entity.Ignore(e => e.Odontologo);
            entity.HasIndex(e => e.IdNumber).IsUnique();
            entity.HasIndex(e => e.OdontologoId);
        });

        // Invoice
        builder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CustomerIdentification).IsRequired().HasMaxLength(32);
            entity.Property(e => e.SriEnvironment).IsRequired().HasMaxLength(20).HasDefaultValue("Pruebas");
            entity.HasMany(e => e.Items)
                .WithOne(e => e.Invoice)
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.IssuedAt);
        });

        // InvoiceItem
        builder.Entity<InvoiceItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
        });

        // Invoice ownership (security boundary per odontologo)
        builder.Entity<OdontologoInvoiceOwnership>(entity =>
        {
            entity.HasKey(e => e.InvoiceId);
            entity.HasOne<Invoice>()
                .WithMany()
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.OdontologoId);
        });

        // AccountingEntry
        builder.Entity<AccountingEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.Type);
        });

        // Accounting ownership (security boundary per odontologo)
        builder.Entity<OdontologoAccountingEntryOwnership>(entity =>
        {
            entity.HasKey(e => e.AccountingEntryId);
            entity.HasOne<AccountingEntry>()
                .WithMany()
                .HasForeignKey(e => e.AccountingEntryId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.OdontologoId);
        });

        // AccountingCategory
        builder.Entity<AccountingCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Type);
        });

        // InventoryItem
        builder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Sku).HasMaxLength(50);
            entity.Property(e => e.Supplier).HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.HasIndex(e => e.OdontologoId);
            entity.HasIndex(e => e.Sku);
        });

        // InventoryAlert
        builder.Entity<InventoryAlert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(500);
            entity.HasOne(e => e.InventoryItem)
                .WithMany()
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.OdontologoId);
            entity.HasIndex(e => e.IsResolved);
            entity.HasIndex(e => e.Type);
        });

        // PurchaseOrder
        builder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Supplier).IsRequired().HasMaxLength(200);
            entity.Property(e => e.InvoiceNumber).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Total).HasPrecision(18, 2);
            entity.HasMany(e => e.Items)
                .WithOne(e => e.PurchaseOrder)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.OdontologoId);
            entity.HasIndex(e => e.PurchaseDate);
            entity.HasIndex(e => e.Status);
        });

        // PurchaseItem
        builder.Entity<PurchaseItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.HasOne(e => e.InventoryItem)
                .WithMany()
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Expense
        builder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
            entity.Property(e => e.InvoiceNumber).HasMaxLength(100);
            entity.Property(e => e.Supplier).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasIndex(e => e.OdontologoId);
            entity.HasIndex(e => e.ExpenseDate);
            entity.HasIndex(e => e.Category);
        });

        // InvoiceConfig
        builder.Entity<InvoiceConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EstablishmentCode).IsRequired().HasMaxLength(3).HasDefaultValue("001");
            entity.Property(e => e.EmissionPoint).IsRequired().HasMaxLength(3).HasDefaultValue("002");
        });

        // TelemedicineSession
        builder.Entity<TelemedicineSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PatientName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Topic).IsRequired().HasMaxLength(250);
            entity.Property(e => e.MeetingLink).HasMaxLength(500);
            entity.HasMany(e => e.Messages)
                .WithOne(e => e.Session)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.OdontologoId);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.ScheduledStartAt);
            entity.HasIndex(e => e.Status);
        });

        // TelemedicineMessage
        builder.Entity<TelemedicineMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SenderRole).IsRequired().HasMaxLength(30);
            entity.Property(e => e.SenderName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.SentAt);
        });

        // PatientPortalPreference
        builder.Entity<PatientPortalPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OdontologoId);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => new { e.OdontologoId, e.PatientId }).IsUnique();
        });

        // PatientPortalNotification
        builder.Entity<PatientPortalNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Channel).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(180);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.ExternalReference).HasMaxLength(200);
            entity.HasIndex(e => e.OdontologoId);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.ScheduledFor);
            entity.HasIndex(e => e.Status);
        });

        // InsuranceClaim
        builder.Entity<InsuranceClaim>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InsurerName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PolicyNumber).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ProcedureCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProcedureDescription).IsRequired().HasMaxLength(300);
            entity.Property(e => e.RequestedAmount).HasPrecision(18, 2);
            entity.Property(e => e.ApprovedAmount).HasPrecision(18, 2);
            entity.Property(e => e.ResponseMessage).HasMaxLength(1000);
            entity.HasIndex(e => e.OdontologoId);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.RequestedAt);
        });

        // SignedClinicalDocument
        builder.Entity<SignedClinicalDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocumentType).IsRequired().HasMaxLength(80);
            entity.Property(e => e.DocumentName).IsRequired().HasMaxLength(300);
            entity.Property(e => e.DocumentHash).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SignatureProvider).IsRequired().HasMaxLength(120);
            entity.Property(e => e.SignatureSerial).HasMaxLength(150);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.HasIndex(e => e.OdontologoId);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.SignedAt);
        });

        // InventoryMovement
        builder.Entity<InventoryMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MovementType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalCost).HasPrecision(18, 2);
            entity.Property(e => e.Reference).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasOne(e => e.InventoryItem)
                .WithMany()
                .HasForeignKey(e => e.InventoryItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.OdontologoId);
            entity.HasIndex(e => e.InventoryItemId);
            entity.HasIndex(e => e.MovementDate);
            entity.HasIndex(e => e.MovementType);
        });
    }
}
