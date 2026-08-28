using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models
{
    public class AgendarEntrevistaViewModel
    {
        [Required]
        public int IdAnalisis { get; set; }

        public string NombreCandidato { get; set; } =
            string.Empty;

        public string TituloVacante { get; set; } =
            string.Empty;

        [Required(ErrorMessage = "Seleccione la fecha")]
        [DataType(DataType.Date)]
        public DateTime FechaEntrevista { get; set; } =
            DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Seleccione la hora")]
        [DataType(DataType.Time)]
        public TimeSpan HoraEntrevista { get; set; } =
            new TimeSpan(9, 0, 0);

        [Required(ErrorMessage = "El entrevistador es obligatorio")]
        [StringLength(150)]
        public string Entrevistador { get; set; } =
            string.Empty;

        [StringLength(1000)]
        [Display(Name = "Comentarios iniciales")]
        public string? ComentariosIniciales { get; set; }

        [Url(ErrorMessage = "Ingrese un enlace válido")]
        [StringLength(500)]
        [Display(Name = "Enlace de entrevista")]
        public string? EnlaceEntrevista { get; set; }
    }
}