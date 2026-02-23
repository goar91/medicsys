using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MEDICSYS.Api.Migrations.Academico
{
    /// <inheritdoc />
    public partial class AddAcademicEnterpriseModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicAccreditationCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Dimension = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    TargetValue = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    CurrentValue = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicAccreditationCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicAccreditationCriteria_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcademicDataAnonymizationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubjectIdentifier = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicDataAnonymizationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicDataAnonymizationRequests_AspNetUsers_RequestedByUs~",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademicDataAnonymizationRequests_AspNetUsers_ReviewedByUse~",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AcademicDataAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Path = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserEmail = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    UserRole = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    SubjectType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    SubjectIdentifier = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    Reason = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicDataAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AcademicDataConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubjectIdentifier = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    LegalBasis = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Granted = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CollectedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicDataConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicDataConsents_AspNetUsers_CollectedByUserId",
                        column: x => x.CollectedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcademicDataRetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataCategory = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RetentionMonths = table.Column<int>(type: "integer", nullable: false),
                    AutoDelete = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ConfiguredByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicDataRetentionPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicDataRetentionPolicies_AspNetUsers_ConfiguredByUserId",
                        column: x => x.ConfiguredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcademicIntegrationConnectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    ProviderType = table.Column<string>(type: "text", nullable: false),
                    EndpointUrl = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    ApiKeyHint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LastMessage = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicIntegrationConnectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicIntegrationConnectors_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcademicStudentRiskFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskLevel = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicStudentRiskFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicStudentRiskFlags_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademicStudentRiskFlags_AspNetUsers_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AcademicStudentRiskFlags_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcademicAccreditationEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EvidenceUrl = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicAccreditationEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicAccreditationEvidences_AcademicAccreditationCriteri~",
                        column: x => x.CriterionId,
                        principalTable: "AcademicAccreditationCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AcademicAccreditationEvidences_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademicAccreditationEvidences_AspNetUsers_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AcademicImprovementPlanActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    Responsible = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProgressPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicImprovementPlanActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicImprovementPlanActions_AcademicAccreditationCriteri~",
                        column: x => x.CriterionId,
                        principalTable: "AcademicAccreditationCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AcademicImprovementPlanActions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcademicIntegrationSyncLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    RecordsProcessed = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicIntegrationSyncLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicIntegrationSyncLogs_AcademicIntegrationConnectors_I~",
                        column: x => x.IntegrationId,
                        principalTable: "AcademicIntegrationConnectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicAccreditationCriteria_Code",
                table: "AcademicAccreditationCriteria",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicAccreditationCriteria_CreatedByUserId",
                table: "AcademicAccreditationCriteria",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicAccreditationCriteria_Dimension",
                table: "AcademicAccreditationCriteria",
                column: "Dimension");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicAccreditationCriteria_Status",
                table: "AcademicAccreditationCriteria",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicAccreditationEvidences_CriterionId",
                table: "AcademicAccreditationEvidences",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicAccreditationEvidences_IsVerified",
                table: "AcademicAccreditationEvidences",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicAccreditationEvidences_UploadedByUserId",
                table: "AcademicAccreditationEvidences",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicAccreditationEvidences_VerifiedByUserId",
                table: "AcademicAccreditationEvidences",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDataAnonymizationRequests_RequestedAt",
                table: "AcademicDataAnonymizationRequests",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDataAnonymizationRequests_RequestedByUserId",
                table: "AcademicDataAnonymizationRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDataAnonymizationRequests_ReviewedByUserId",
                table: "AcademicDataAnonymizationRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDataAnonymizationRequests_Status",
                table: "AcademicDataAnonymizationRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDataAuditEvents_EventType",
                table: "AcademicDataAuditEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDataAuditEvents_OccurredAt",
                table: "AcademicDataAuditEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDataAuditEvents_UserId",
                table: "AcademicDataAuditEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDataConsents_CollectedByUserId",
                table: "AcademicDataConsents",
                column: "CollectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDataConsents_GrantedAt",
                table: "AcademicDataConsents",
                column: "GrantedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDataConsents_SubjectType_SubjectIdentifier",
                table: "AcademicDataConsents",
                columns: new[] { "SubjectType", "SubjectIdentifier" });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDataRetentionPolicies_ConfiguredByUserId",
                table: "AcademicDataRetentionPolicies",
                column: "ConfiguredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDataRetentionPolicies_DataCategory",
                table: "AcademicDataRetentionPolicies",
                column: "DataCategory",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicDataRetentionPolicies_IsActive",
                table: "AcademicDataRetentionPolicies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicImprovementPlanActions_CreatedByUserId",
                table: "AcademicImprovementPlanActions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicImprovementPlanActions_CriterionId",
                table: "AcademicImprovementPlanActions",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicImprovementPlanActions_DueDate",
                table: "AcademicImprovementPlanActions",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicImprovementPlanActions_Status",
                table: "AcademicImprovementPlanActions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicIntegrationConnectors_CreatedByUserId",
                table: "AcademicIntegrationConnectors",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicIntegrationConnectors_Enabled",
                table: "AcademicIntegrationConnectors",
                column: "Enabled");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicIntegrationConnectors_Name",
                table: "AcademicIntegrationConnectors",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicIntegrationConnectors_ProviderType",
                table: "AcademicIntegrationConnectors",
                column: "ProviderType");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicIntegrationSyncLogs_IntegrationId",
                table: "AcademicIntegrationSyncLogs",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicIntegrationSyncLogs_StartedAt",
                table: "AcademicIntegrationSyncLogs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicStudentRiskFlags_CreatedByUserId",
                table: "AcademicStudentRiskFlags",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicStudentRiskFlags_IsResolved",
                table: "AcademicStudentRiskFlags",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicStudentRiskFlags_ResolvedByUserId",
                table: "AcademicStudentRiskFlags",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicStudentRiskFlags_RiskLevel",
                table: "AcademicStudentRiskFlags",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicStudentRiskFlags_StudentId",
                table: "AcademicStudentRiskFlags",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicAccreditationEvidences");

            migrationBuilder.DropTable(
                name: "AcademicDataAnonymizationRequests");

            migrationBuilder.DropTable(
                name: "AcademicDataAuditEvents");

            migrationBuilder.DropTable(
                name: "AcademicDataConsents");

            migrationBuilder.DropTable(
                name: "AcademicDataRetentionPolicies");

            migrationBuilder.DropTable(
                name: "AcademicImprovementPlanActions");

            migrationBuilder.DropTable(
                name: "AcademicIntegrationSyncLogs");

            migrationBuilder.DropTable(
                name: "AcademicStudentRiskFlags");

            migrationBuilder.DropTable(
                name: "AcademicAccreditationCriteria");

            migrationBuilder.DropTable(
                name: "AcademicIntegrationConnectors");
        }
    }
}
