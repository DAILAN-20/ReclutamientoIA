using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReclutamientoIA.Migrations
{
    /// <inheritdoc />
    public partial class CorregirRelacionesAnalisisIA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalisisIA_Candidatos_CandidatoIdCandidato",
                table: "AnalisisIA");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalisisIA_Vacantes_VacanteIdVacante",
                table: "AnalisisIA");

            migrationBuilder.DropIndex(
                name: "IX_AnalisisIA_CandidatoIdCandidato",
                table: "AnalisisIA");

            migrationBuilder.DropIndex(
                name: "IX_AnalisisIA_VacanteIdVacante",
                table: "AnalisisIA");

            migrationBuilder.DropColumn(
                name: "CandidatoIdCandidato",
                table: "AnalisisIA");

            migrationBuilder.DropColumn(
                name: "VacanteIdVacante",
                table: "AnalisisIA");

            migrationBuilder.CreateIndex(
                name: "IX_AnalisisIA_IdCandidato",
                table: "AnalisisIA",
                column: "IdCandidato");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalisisIA_Candidatos_IdCandidato",
                table: "AnalisisIA",
                column: "IdCandidato",
                principalTable: "Candidatos",
                principalColumn: "IdCandidato",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AnalisisIA_Vacantes_IdVacante",
                table: "AnalisisIA",
                column: "IdVacante",
                principalTable: "Vacantes",
                principalColumn: "IdVacante",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalisisIA_Candidatos_IdCandidato",
                table: "AnalisisIA");

            migrationBuilder.DropForeignKey(
                name: "FK_AnalisisIA_Vacantes_IdVacante",
                table: "AnalisisIA");

            migrationBuilder.DropIndex(
                name: "IX_AnalisisIA_IdCandidato",
                table: "AnalisisIA");

            migrationBuilder.AddColumn<int>(
                name: "CandidatoIdCandidato",
                table: "AnalisisIA",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VacanteIdVacante",
                table: "AnalisisIA",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AnalisisIA_CandidatoIdCandidato",
                table: "AnalisisIA",
                column: "CandidatoIdCandidato");

            migrationBuilder.CreateIndex(
                name: "IX_AnalisisIA_VacanteIdVacante",
                table: "AnalisisIA",
                column: "VacanteIdVacante");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalisisIA_Candidatos_CandidatoIdCandidato",
                table: "AnalisisIA",
                column: "CandidatoIdCandidato",
                principalTable: "Candidatos",
                principalColumn: "IdCandidato",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AnalisisIA_Vacantes_VacanteIdVacante",
                table: "AnalisisIA",
                column: "VacanteIdVacante",
                principalTable: "Vacantes",
                principalColumn: "IdVacante",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
