using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models
{
    public class ConfiguracionIAViewModel
    {
        public int IdConfiguracion { get; set; }

        [Required(ErrorMessage = "Seleccione el proveedor de IA.")]
        [Display(Name = "Proveedor")]
        public string Proveedor { get; set; } = "OpenAI";

        [Required(ErrorMessage = "Ingrese el modelo de IA.")]
        [StringLength(100)]
        [Display(Name = "Modelo")]
        public string Modelo { get; set; } = "gpt-4.1-mini";

        [DataType(DataType.Password)]
        [Display(Name = "Clave API")]
        public string? ClaveApi { get; set; }

        public bool TieneClaveGuardada { get; set; }
    }
}