using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MEDICSYS.Api.Migrations.Odontologia
{
    /// <inheritdoc />
    public partial class AddOdontologoInnovationModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InsuranceClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OdontologoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    InsurerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PolicyNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProcedureCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProcedureDescription = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResponseMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceClaims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OdontologoAccountingEntryOwnerships",
                columns: table => new
                {
                    AccountingEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    OdontologoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdontologoAccountingEntryOwnerships", x => x.AccountingEntryId);
                    table.ForeignKey(
                        name: "FK_OdontologoAccountingEntryOwnerships_AccountingEntries_Accou~",
                        column: x => x.AccountingEntryId,
                        principalTable: "AccountingEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OdontologoInvoiceOwnerships",
                columns: table => new
                {
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OdontologoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdontologoInvoiceOwnerships", x => x.InvoiceId);
                    table.ForeignKey(
                        name: "FK_OdontologoInvoiceOwnerships_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientPortalNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OdontologoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Subject = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientPortalNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatientPortalPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OdontologoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WhatsAppEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientPortalPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SignedClinicalDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OdontologoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DocumentName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DocumentHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SignatureProvider = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SignatureSerial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignedClinicalDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelemedicineSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OdontologoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    PatientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Topic = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    MeetingLink = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ScheduledStartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledEndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemedicineSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelemedicineMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SenderName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemedicineMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelemedicineMessages_TelemedicineSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "TelemedicineSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_OdontologoId",
                table: "InsuranceClaims",
                column: "OdontologoId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_PatientId",
                table: "InsuranceClaims",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_RequestedAt",
                table: "InsuranceClaims",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_Status",
                table: "InsuranceClaims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OdontologoAccountingEntryOwnerships_OdontologoId",
                table: "OdontologoAccountingEntryOwnerships",
                column: "OdontologoId");

            migrationBuilder.CreateIndex(
                name: "IX_OdontologoInvoiceOwnerships_OdontologoId",
                table: "OdontologoInvoiceOwnerships",
                column: "OdontologoId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientPortalNotifications_OdontologoId",
                table: "PatientPortalNotifications",
                column: "OdontologoId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientPortalNotifications_PatientId",
                table: "PatientPortalNotifications",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientPortalNotifications_ScheduledFor",
                table: "PatientPortalNotifications",
                column: "ScheduledFor");

            migrationBuilder.CreateIndex(
                name: "IX_PatientPortalNotifications_Status",
                table: "PatientPortalNotifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PatientPortalPreferences_OdontologoId",
                table: "PatientPortalPreferences",
                column: "OdontologoId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientPortalPreferences_OdontologoId_PatientId",
                table: "PatientPortalPreferences",
                columns: new[] { "OdontologoId", "PatientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientPortalPreferences_PatientId",
                table: "PatientPortalPreferences",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_SignedClinicalDocuments_OdontologoId",
                table: "SignedClinicalDocuments",
                column: "OdontologoId");

            migrationBuilder.CreateIndex(
                name: "IX_SignedClinicalDocuments_PatientId",
                table: "SignedClinicalDocuments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_SignedClinicalDocuments_SignedAt",
                table: "SignedClinicalDocuments",
                column: "SignedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TelemedicineMessages_SentAt",
                table: "TelemedicineMessages",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_TelemedicineMessages_SessionId",
                table: "TelemedicineMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TelemedicineSessions_OdontologoId",
                table: "TelemedicineSessions",
                column: "OdontologoId");

            migrationBuilder.CreateIndex(
                name: "IX_TelemedicineSessions_PatientId",
                table: "TelemedicineSessions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_TelemedicineSessions_ScheduledStartAt",
                table: "TelemedicineSessions",
                column: "ScheduledStartAt");

            migrationBuilder.CreateIndex(
                name: "IX_TelemedicineSessions_Status",
                table: "TelemedicineSessions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsuranceClaims");

            migrationBuilder.DropTable(
                name: "OdontologoAccountingEntryOwnerships");

            migrationBuilder.DropTable(
                name: "OdontologoInvoiceOwnerships");

            migrationBuilder.DropTable(
                name: "PatientPortalNotifications");

            migrationBuilder.DropTable(
                name: "PatientPortalPreferences");

            migrationBuilder.DropTable(
                name: "SignedClinicalDocuments");

            migrationBuilder.DropTable(
                name: "TelemedicineMessages");

            migrationBuilder.DropTable(
                name: "TelemedicineSessions");
        }
    }
}
