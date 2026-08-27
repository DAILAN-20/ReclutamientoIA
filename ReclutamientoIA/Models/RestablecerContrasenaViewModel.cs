using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models
{
    public class RestablecerContrasenaViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "Debe contener al menos 8 caracteres"
        )]
        [DataType(DataType.Password)]
        public string NuevaContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme la nueva contraseña")]
        [DataType(DataType.Password)]
        [Compare(
            nameof(NuevaContrasena),
            ErrorMessage = "Las contraseñas no coinciden"
        )]
        public string ConfirmarContrasena { get; set; } = string.Empty;
    }
}