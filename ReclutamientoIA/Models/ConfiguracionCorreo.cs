using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models
{
    public class ConfiguracionCorreo
    {
        [Key]
        public int IdConfiguracionCorreo { get; set; }

        [Required]
        [StringLength(200)]
        public string ServidorSmtp { get; set; }
            = "smtp.gmail.com";

        [Range(1, 65535)]
        public int PuertoSmtp { get; set; }
            = 587;

        [Required]
        [StringLength(200)]
        public string ServidorImap { get; set; }
            = "imap.gmail.com";

        [Range(1, 65535)]
        public int PuertoImap { get; set; }
            = 993;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Usuario { get; set; }
            = string.Empty;

        [StringLength(4000)]
        public string? ClaveProtegida { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string CorreoRemitente { get; set; }
            = string.Empty;

        [Required]
        [StringLength(150)]
        public string NombreRemitente { get; set; }
            = "Reclutamiento IA";

        public bool Activo { get; set; }
            = true;

        public DateTime FechaActualizacion { get; set; }
            = DateTime.Now;
    }
}