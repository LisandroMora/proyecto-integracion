using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nomina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPorcentajeToTiposCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Porcentaje",
                table: "TiposIngreso",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Porcentaje",
                table: "TiposDeduccion",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Porcentaje",
                table: "TiposIngreso");

            migrationBuilder.DropColumn(
                name: "Porcentaje",
                table: "TiposDeduccion");
        }
    }
}
