using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReclutamientoIA.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConfiguracionYPromptsIA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionesIA",
                columns: table => new
                {
                    IdConfiguracion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Proveedor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Modelo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Temperatura = table.Column<double>(type: "float", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesIA", x => x.IdConfiguracion);
                });

            migrationBuilder.CreateTable(
                name: "PromptsIA",
                columns: table => new
                {
                    IdPrompt = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptsIA", x => x.IdPrompt);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionesIA");

            migrationBuilder.DropTable(
                name: "PromptsIA");
        }
    }
}
