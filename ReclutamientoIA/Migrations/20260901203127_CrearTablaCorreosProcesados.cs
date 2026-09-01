using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReclutamientoIA.Migrations
{
    /// <inheritdoc />
    public partial class CrearTablaCorreosProcesados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CorreosProcesados",
                columns: table => new
                {
                    IdCorreoProcesado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuentaCorreo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Carpeta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UidValidity = table.Column<long>(type: "bigint", nullable: false),
                    UidCorreo = table.Column<long>(type: "bigint", nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Asunto = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CantidadPdf = table.Column<int>(type: "int", nullable: false),
                    FechaProcesamiento = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorreosProcesados", x => x.IdCorreoProcesado);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorreosProcesados_CuentaCorreo_Carpeta_UidValidity_UidCorreo",
                table: "CorreosProcesados",
                columns: new[] { "CuentaCorreo", "Carpeta", "UidValidity", "UidCorreo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorreosProcesados");
        }
    }
}
