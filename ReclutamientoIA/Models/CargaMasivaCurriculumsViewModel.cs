using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ReclutamientoIA.Models;

public class CargaMasivaCurriculumsViewModel
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Seleccione una vacante."
    )]
    public int IdVacante { get; set; }

    public string TituloVacante { get; set; } =
        string.Empty;

    [Required(
        ErrorMessage = "Seleccione al menos un currículum."
    )]
    public List<IFormFile> Archivos { get; set; } =
        new();
}
