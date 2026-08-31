using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models;

public class CorreoOptions
{
    public const string Seccion = "Correo";

    [Required]
    public string Servidor { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Puerto { get; set; } = 587;

    [Required]
    public string Usuario { get; set; } = string.Empty;

    [Required]
    public string Clave { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string CorreoRemitente { get; set; } = string.Empty;

    [Required]
    public string NombreRemitente { get; set; } =
        "Reclutamiento IA";
}