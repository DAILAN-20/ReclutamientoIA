using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ReclutamientoIA.Models
{
    [Index(nameof(HashArchivo), IsUnique = true)]
    public class Candidato
    {
        [Key]
        public int IdCandidato { get; set; }

        [StringLength(150)]
        public string? NombreCompleto { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Correo { get; set; }

        [StringLength(30)]
        public string? Telefono { get; set; }

        [Required]
        [StringLength(255)]
        public string NombreArchivoOriginal { get; set; } =
            string.Empty;

        [Required]
        [StringLength(255)]
        public string NombreArchivoGuardado { get; set; } =
            string.Empty;

        [Required]
        [StringLength(100)]
        public string TipoContenido { get; set; } =
            "application/pdf";

        public long TamanoArchivo { get; set; }

        [Required]
        [StringLength(64)]
        public string HashArchivo { get; set; } =
            string.Empty;

        [StringLength(20)]
        public string? OrigenRegistro { get; set; } =
    "Manual";

        [StringLength(250)]
        public string? AsuntoCorreo { get; set; }

        public DateTime? FechaCorreo { get; set; }

        public DateTime FechaRegistro { get; set; } =
            DateTime.Now;

        public bool Activo { get; set; } = true;
    }
}