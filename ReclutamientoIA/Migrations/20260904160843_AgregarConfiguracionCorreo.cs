using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReclutamientoIA.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConfiguracionCorreo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionesCorreo",
                columns: table => new
                {
                    IdConfiguracionCorreo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServidorSmtp = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PuertoSmtp = table.Column<int>(type: "int", nullable: false),
                    ServidorImap = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PuertoImap = table.Column<int>(type: "int", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ClaveProtegida = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CorreoRemitente = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NombreRemitente = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesCorreo", x => x.IdConfiguracionCorreo);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionesCorreo");
        }
    }
}
