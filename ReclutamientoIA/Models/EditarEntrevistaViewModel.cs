using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models
{
    public class EditarEntrevistaViewModel
    {
        [Required]
        public int IdEntrevista { get; set; }

        public string NombreCandidato { get; set; } =
            string.Empty;

        public string TituloVacante { get; set; } =
            string.Empty;

        [Required(ErrorMessage = "Seleccione la fecha")]
        [DataType(DataType.Date)]
        public DateTime FechaEntrevista { get; set; }

        [Required(ErrorMessage = "Seleccione la hora")]
        [DataType(DataType.Time)]
        public TimeSpan HoraEntrevista { get; set; }

        [Required(ErrorMessage = "El entrevistador es obligatorio")]
        [StringLength(150)]
        public string Entrevistador { get; set; } =
            string.Empty;

        [StringLength(1000)]
        public string? ComentariosIniciales { get; set; }

        [Url(ErrorMessage = "Ingrese un enlace válido")]
        [StringLength(500)]
        public string? EnlaceEntrevista { get; set; }
    }
}