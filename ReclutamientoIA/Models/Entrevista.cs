using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReclutamientoIA.Models
{
    [Index(nameof(IdAnalisis), IsUnique = true)]
    public class Entrevista
    {
        [Key]
        public int IdEntrevista { get; set; }

        [Required]
        [ForeignKey(nameof(AnalisisIA))]
        public int IdAnalisis { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime FechaEntrevista { get; set; }

        [Required]
        [Column(TypeName = "time")]
        public TimeSpan HoraEntrevista { get; set; }

        [Required]
        [StringLength(150)]
        public string Entrevistador { get; set; } =
            string.Empty;

        [StringLength(1000)]
        public string? ComentariosIniciales { get; set; }

        [StringLength(1000)]
        public string? ComentariosFinales { get; set; }

        [Range(0, 100)]
        public int? CalificacionManual { get; set; }

        [Required]
        [StringLength(30)]
        public string EstadoEntrevista { get; set; } =
            "Pendiente";

        [StringLength(30)]
        public string? Resultado { get; set; }

        [StringLength(500)]
        public string? EnlaceEntrevista { get; set; }

        [StringLength(255)]
        public string? GoogleEventId { get; set; }

        public DateTime FechaCreacion { get; set; } =
            DateTime.Now;

        public AnalisisIA AnalisisIA { get; set; } = null!;
    }
}