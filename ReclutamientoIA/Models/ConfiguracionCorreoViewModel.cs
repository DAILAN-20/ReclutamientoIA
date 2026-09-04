using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models
{
    public class ConfiguracionCorreoViewModel
    {
        public int IdConfiguracionCorreo { get; set; }

        [Required(
            ErrorMessage =
                "Ingrese el servidor SMTP."
        )]
        [Display(Name = "Servidor SMTP")]
        public string ServidorSmtp { get; set; }
            = "smtp.gmail.com";

        [Required(
            ErrorMessage =
                "Ingrese el puerto SMTP."
        )]
        [Range(
            1,
            65535,
            ErrorMessage =
                "Ingrese un puerto SMTP válido."
        )]
        [Display(Name = "Puerto SMTP")]
        public int PuertoSmtp { get; set; }
            = 587;

        [Required(
            ErrorMessage =
                "Ingrese el servidor IMAP."
        )]
        [Display(Name = "Servidor IMAP")]
        public string ServidorImap { get; set; }
            = "imap.gmail.com";

        [Required(
            ErrorMessage =
                "Ingrese el puerto IMAP."
        )]
        [Range(
            1,
            65535,
            ErrorMessage =
                "Ingrese un puerto IMAP válido."
        )]
        [Display(Name = "Puerto IMAP")]
        public int PuertoImap { get; set; }
            = 993;

        [Required(
            ErrorMessage =
                "Ingrese el correo de usuario."
        )]
        [EmailAddress(
            ErrorMessage =
                "Ingrese un correo válido."
        )]
        [Display(Name = "Usuario")]
        public string Usuario { get; set; }
            = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Contraseña de aplicación")]
        public string? Clave { get; set; }

        [Required(
            ErrorMessage =
                "Ingrese el correo remitente."
        )]
        [EmailAddress(
            ErrorMessage =
                "Ingrese un correo remitente válido."
        )]
        [Display(Name = "Correo remitente")]
        public string CorreoRemitente { get; set; }
            = string.Empty;

        [Required(
            ErrorMessage =
                "Ingrese el nombre del remitente."
        )]
        [StringLength(150)]
        [Display(Name = "Nombre del remitente")]
        public string NombreRemitente { get; set; }
            = "Reclutamiento IA";

        public bool TieneClaveGuardada { get; set; }
    }
}