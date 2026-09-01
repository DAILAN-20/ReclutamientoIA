using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models;

public class CorreoOptions
{
    public const string Seccion = "Correo";

    // ==========================================
    // ENVÍO DE CORREOS - SMTP
    // ==========================================

    [Required]
    public string Servidor { get; set; } =
        "smtp.gmail.com";

    [Range(1, 65535)]
    public int Puerto { get; set; } = 587;

    // ==========================================
    // RECEPCIÓN DE CORREOS - IMAP
    // ==========================================

    [Required]
    public string ServidorImap { get; set; } =
        "imap.gmail.com";

    [Range(1, 65535)]
    public int PuertoImap { get; set; } = 993;

    // ==========================================
    // CREDENCIALES
    // ==========================================

    [Required]
    [EmailAddress]
    public string Usuario { get; set; } =
        string.Empty;

    [Required]
    public string Clave { get; set; } =
        string.Empty;

    [Required]
    [EmailAddress]
    public string CorreoRemitente { get; set; } =
        string.Empty;

    [Required]
    public string NombreRemitente { get; set; } =
        "Reclutamiento IA";
}