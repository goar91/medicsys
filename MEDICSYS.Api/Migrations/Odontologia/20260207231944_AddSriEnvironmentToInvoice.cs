using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MEDICSYS.Api.Migrations.Odontologia
{
    /// <inheritdoc />
    public partial class AddSriEnvironmentToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SriEnvironment",
                table: "Invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pruebas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SriEnvironment",
                table: "Invoices");
        }
    }
}
