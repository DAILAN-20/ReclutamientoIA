using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReclutamientoIA.Migrations
{
    /// <inheritdoc />
    public partial class AgregarOrigenCorreoCandidatos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AsuntoCorreo",
                table: "Candidatos",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCorreo",
                table: "Candidatos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrigenRegistro",
                table: "Candidatos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AsuntoCorreo",
                table: "Candidatos");

            migrationBuilder.DropColumn(
                name: "FechaCorreo",
                table: "Candidatos");

            migrationBuilder.DropColumn(
                name: "OrigenRegistro",
                table: "Candidatos");
        }
    }
}
