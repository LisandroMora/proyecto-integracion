using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nomina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VincularTransaccionConAsiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AsientosContables_Anio_Mes",
                table: "AsientosContables");

            migrationBuilder.DropIndex(
                name: "IX_AsientosContables_Anio_Mes_TipoTransaccion_ConceptoId",
                table: "AsientosContables");

            migrationBuilder.AddColumn<int>(
                name: "AsientoContableId",
                table: "Transacciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_AsientoContableId",
                table: "Transacciones",
                column: "AsientoContableId");

            migrationBuilder.CreateIndex(
                name: "IX_Transacciones_Fecha_AsientoContableId",
                table: "Transacciones",
                columns: new[] { "Fecha", "AsientoContableId" });

            migrationBuilder.CreateIndex(
                name: "IX_AsientosContables_Anio_Mes_TipoTransaccion_ConceptoId",
                table: "AsientosContables",
                columns: new[] { "Anio", "Mes", "TipoTransaccion", "ConceptoId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Transacciones_AsientosContables_AsientoContableId",
                table: "Transacciones",
                column: "AsientoContableId",
                principalTable: "AsientosContables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transacciones_AsientosContables_AsientoContableId",
                table: "Transacciones");

            migrationBuilder.DropIndex(
                name: "IX_Transacciones_AsientoContableId",
                table: "Transacciones");

            migrationBuilder.DropIndex(
                name: "IX_Transacciones_Fecha_AsientoContableId",
                table: "Transacciones");

            migrationBuilder.DropIndex(
                name: "IX_AsientosContables_Anio_Mes_TipoTransaccion_ConceptoId",
                table: "AsientosContables");

            migrationBuilder.DropColumn(
                name: "AsientoContableId",
                table: "Transacciones");

            migrationBuilder.CreateIndex(
                name: "IX_AsientosContables_Anio_Mes",
                table: "AsientosContables",
                columns: new[] { "Anio", "Mes" });

            migrationBuilder.CreateIndex(
                name: "IX_AsientosContables_Anio_Mes_TipoTransaccion_ConceptoId",
                table: "AsientosContables",
                columns: new[] { "Anio", "Mes", "TipoTransaccion", "ConceptoId" },
                unique: true,
                filter: "[Estado] = 1");
        }
    }
}
