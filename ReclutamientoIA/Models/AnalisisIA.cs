using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReclutamientoIA.Models
{
    [Index(
        nameof(IdVacante),
        nameof(IdCandidato),
        IsUnique = true
    )]
    public class AnalisisIA
    {
        [Key]
        public int IdAnalisis { get; set; }

        [Required]
        [ForeignKey(nameof(Vacante))]
        public int IdVacante { get; set; }

        [Required]
        [ForeignKey(nameof(Candidato))]
        public int IdCandidato { get; set; }

        [Range(0, 100)]
        public int Puntuacion { get; set; }

        public string Resumen { get; set; } = string.Empty;

        public string Fortalezas { get; set; } = string.Empty;

        public string Debilidades { get; set; } = string.Empty;

        public string Recomendacion { get; set; } = string.Empty;

        [StringLength(50)]
        public string EstadoProceso { get; set; } =
            "Analizado";

        [StringLength(50)]
        public string ProveedorIA { get; set; } =
            string.Empty;

        [StringLength(100)]
        public string ModeloIA { get; set; } =
            string.Empty;

        public DateTime FechaAnalisis { get; set; } =
            DateTime.Now;

        public Vacante Vacante { get; set; } = null!;

        public Candidato Candidato { get; set; } = null!;
    }
}