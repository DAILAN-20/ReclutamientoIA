using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models;

[Index(
    nameof(CuentaCorreo),
    nameof(Carpeta),
    nameof(UidValidity),
    nameof(UidCorreo),
    IsUnique = true
)]
public class CorreoProcesado
{
    [Key]
    public int IdCorreoProcesado { get; set; }

    [Required]
    [StringLength(150)]
    public string CuentaCorreo { get; set; } =
        string.Empty;

    [Required]
    [StringLength(100)]
    public string Carpeta { get; set; } =
        "INBOX";

    public long UidValidity { get; set; }

    public long UidCorreo { get; set; }

    [StringLength(500)]
    public string? MessageId { get; set; }

    [StringLength(250)]
    public string? Asunto { get; set; }

    public int CantidadPdf { get; set; }

    public DateTime FechaProcesamiento { get; set; } =
        DateTime.Now;
}