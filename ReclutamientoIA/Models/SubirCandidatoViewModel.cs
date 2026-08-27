using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models
{
    public class SubirCandidatoViewModel
    {
        [StringLength(150)]
        [Display(Name = "Nombre completo")]
        public string? NombreCompleto { get; set; }

        [EmailAddress(ErrorMessage = "Ingrese un correo válido")]
        [StringLength(150)]
        [Display(Name = "Correo electrónico")]
        public string? Correo { get; set; }

        [StringLength(30)]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "Seleccione un currículum PDF")]
        [Display(Name = "Currículum")]
        public IFormFile ArchivoCv { get; set; } = null!;
    }
}