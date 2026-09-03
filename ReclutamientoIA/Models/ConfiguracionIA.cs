using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models
{
    public class ConfiguracionIA
    {
        [Key]
        public int IdConfiguracion { get; set; }

        [Required]
        [StringLength(30)]
        public string Proveedor { get; set; } = "OpenAI";

        [Required]
        [StringLength(100)]
        public string Modelo { get; set; } = "gpt-4.1-mini";

        // La clave API se guardará cifrada.
        [StringLength(4000)]
        public string? ClaveApiProtegida { get; set; }

        // Se conservan porque el sistema actual las utiliza,
        // pero no aparecerán en el formulario.
        [Range(0, 2)]
        public double Temperatura { get; set; } = 0.2;

        public bool Activo { get; set; } = true;

        public DateTime FechaActualizacion { get; set; }
            = DateTime.Now;
    }
}