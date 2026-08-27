using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models
{
    public class Vacante
    {
        [Key]
        public int IdVacante { get; set; }


        [Required(ErrorMessage = "El título es obligatorio")]
        [StringLength(150)]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria")]
        public string Descripcion { get; set; } = string.Empty;

        public string Requisitos { get; set; } = string.Empty;

        public string Estado { get; set; } = "Activa";

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}