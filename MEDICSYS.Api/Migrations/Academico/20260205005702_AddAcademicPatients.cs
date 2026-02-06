using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MEDICSYS.Api.Migrations.Academico
{
    /// <inheritdoc />
    public partial class AddAcademicPatients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicPatients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BloodType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Allergies = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MedicalConditions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EmergencyContact = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EmergencyPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByProfessorId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicPatients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicPatients_AspNetUsers_CreatedByProfessorId",
                        column: x => x.CreatedByProfessorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPatients_CreatedByProfessorId",
                table: "AcademicPatients",
                column: "CreatedByProfessorId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPatients_IdNumber",
                table: "AcademicPatients",
                column: "IdNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicPatients");
        }
    }
}
