using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReclutamientoIA.Migrations
{
    /// <inheritdoc />
    public partial class CrearTablaEntrevistas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Entrevistas",
                columns: table => new
                {
                    IdEntrevista = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdAnalisis = table.Column<int>(type: "int", nullable: false),
                    FechaEntrevista = table.Column<DateTime>(type: "date", nullable: false),
                    HoraEntrevista = table.Column<TimeSpan>(type: "time", nullable: false),
                    Entrevistador = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ComentariosIniciales = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ComentariosFinales = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CalificacionManual = table.Column<int>(type: "int", nullable: true),
                    EstadoEntrevista = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EnlaceEntrevista = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GoogleEventId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entrevistas", x => x.IdEntrevista);
                    table.ForeignKey(
                        name: "FK_Entrevistas_AnalisisIA_IdAnalisis",
                        column: x => x.IdAnalisis,
                        principalTable: "AnalisisIA",
                        principalColumn: "IdAnalisis",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entrevistas_IdAnalisis",
                table: "Entrevistas",
                column: "IdAnalisis",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entrevistas");
        }
    }
}
