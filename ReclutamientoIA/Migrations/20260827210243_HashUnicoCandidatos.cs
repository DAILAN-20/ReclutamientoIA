using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReclutamientoIA.Migrations
{
    /// <inheritdoc />
    public partial class HashUnicoCandidatos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Candidatos_HashArchivo",
                table: "Candidatos",
                column: "HashArchivo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Candidatos_HashArchivo",
                table: "Candidatos");
        }
    }
}
