using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models
{
    public class EditarUsuarioViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido")]
        public string Correo { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Ingrese un teléfono válido")]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "Seleccione un rol")]
        public string Rol { get; set; } = string.Empty;

        public bool Activo { get; set; }
    }
}