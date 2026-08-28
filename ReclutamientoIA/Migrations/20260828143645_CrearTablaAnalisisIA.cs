using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReclutamientoIA.Migrations
{
    /// <inheritdoc />
    public partial class CrearTablaAnalisisIA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalisisIA",
                columns: table => new
                {
                    IdAnalisis = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdVacante = table.Column<int>(type: "int", nullable: false),
                    IdCandidato = table.Column<int>(type: "int", nullable: false),
                    Puntuacion = table.Column<int>(type: "int", nullable: false),
                    Resumen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fortalezas = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Debilidades = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Recomendacion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstadoProceso = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProveedorIA = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModeloIA = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaAnalisis = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VacanteIdVacante = table.Column<int>(type: "int", nullable: false),
                    CandidatoIdCandidato = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalisisIA", x => x.IdAnalisis);
                    table.ForeignKey(
                        name: "FK_AnalisisIA_Candidatos_CandidatoIdCandidato",
                        column: x => x.CandidatoIdCandidato,
                        principalTable: "Candidatos",
                        principalColumn: "IdCandidato",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnalisisIA_Vacantes_VacanteIdVacante",
                        column: x => x.VacanteIdVacante,
                        principalTable: "Vacantes",
                        principalColumn: "IdVacante",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalisisIA_CandidatoIdCandidato",
                table: "AnalisisIA",
                column: "CandidatoIdCandidato");

            migrationBuilder.CreateIndex(
                name: "IX_AnalisisIA_IdVacante_IdCandidato",
                table: "AnalisisIA",
                columns: new[] { "IdVacante", "IdCandidato" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalisisIA_VacanteIdVacante",
                table: "AnalisisIA",
                column: "VacanteIdVacante");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalisisIA");
        }
    }
}
