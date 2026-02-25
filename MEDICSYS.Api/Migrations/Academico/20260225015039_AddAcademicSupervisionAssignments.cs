using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MEDICSYS.Api.Migrations.Academico
{
    /// <inheritdoc />
    public partial class AddAcademicSupervisionAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicSupervisionAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfessorId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicSupervisionAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicSupervisionAssignments_AcademicPatients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AcademicPatients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AcademicSupervisionAssignments_AspNetUsers_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademicSupervisionAssignments_AspNetUsers_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademicSupervisionAssignments_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicSupervisionAssignments_AssignedByUserId",
                table: "AcademicSupervisionAssignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicSupervisionAssignments_IsActive",
                table: "AcademicSupervisionAssignments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicSupervisionAssignments_PatientId",
                table: "AcademicSupervisionAssignments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicSupervisionAssignments_ProfessorId",
                table: "AcademicSupervisionAssignments",
                column: "ProfessorId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicSupervisionAssignments_ProfessorId_StudentId_Patien~",
                table: "AcademicSupervisionAssignments",
                columns: new[] { "ProfessorId", "StudentId", "PatientId" });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicSupervisionAssignments_StudentId",
                table: "AcademicSupervisionAssignments",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicSupervisionAssignments");
        }
    }
}
