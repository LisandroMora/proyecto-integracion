using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nomina.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarVerificacionAsientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstadoVerificacion",
                table: "AsientosContables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVerificacion",
                table: "AsientosContables",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MensajeVerificacion",
                table: "AsientosContables",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstadoVerificacion",
                table: "AsientosContables");

            migrationBuilder.DropColumn(
                name: "FechaVerificacion",
                table: "AsientosContables");

            migrationBuilder.DropColumn(
                name: "MensajeVerificacion",
                table: "AsientosContables");
        }
    }
}
