using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MEDICSYS.Api.Migrations.Academico
{
    [DbContext(typeof(MEDICSYS.Api.Data.AcademicDbContext))]
    [Migration("20260223190000_AddProfessorReviewEnhancements")]
    public class AddProfessorReviewEnhancements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Grade",
                table: "AcademicClinicalHistories",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "AcademicClinicalHistories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AcademicReviewCommentTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfessorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CommentText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicReviewCommentTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicReviewCommentTemplates_AspNetUsers_ProfessorId",
                        column: x => x.ProfessorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicClinicalHistories_SubmittedAt",
                table: "AcademicClinicalHistories",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicReviewCommentTemplates_ProfessorId",
                table: "AcademicReviewCommentTemplates",
                column: "ProfessorId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicReviewCommentTemplates_ProfessorId_Title",
                table: "AcademicReviewCommentTemplates",
                columns: new[] { "ProfessorId", "Title" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicReviewCommentTemplates");

            migrationBuilder.DropIndex(
                name: "IX_AcademicClinicalHistories_SubmittedAt",
                table: "AcademicClinicalHistories");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "AcademicClinicalHistories");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "AcademicClinicalHistories");
        }
    }
}
