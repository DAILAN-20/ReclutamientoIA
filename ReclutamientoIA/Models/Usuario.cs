using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models
{
    public class Usuario : IdentityUser
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        [StringLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}