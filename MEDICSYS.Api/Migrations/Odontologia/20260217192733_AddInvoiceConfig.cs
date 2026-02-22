using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MEDICSYS.Api.Migrations.Odontologia
{
    /// <inheritdoc />
    public partial class AddInvoiceConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EstablishmentCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "001"),
                    EmissionPoint = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "002"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceConfigs");
        }
    }
}
