using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models
{
    public class PromptIA
    {
        [Key]
        public int IdPrompt { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } =
            "Análisis de currículum";

        [Required]
        [StringLength(50)]
        public string Tipo { get; set; } =
            "AnalisisCV";

        [Required]
        public string Contenido { get; set; } =
            string.Empty;

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } =
            DateTime.Now;

        public DateTime FechaActualizacion { get; set; } =
            DateTime.Now;
    }
}