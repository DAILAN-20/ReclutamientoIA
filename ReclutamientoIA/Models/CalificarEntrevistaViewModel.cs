using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ReclutamientoIA.Models;

public class CalificarEntrevistaViewModel
{
    [Range(1, int.MaxValue)]
    public int IdEntrevista { get; set; }

    [ValidateNever]
    public string NombreCandidato { get; set; } =
        string.Empty;

    [ValidateNever]
    public string TituloVacante { get; set; } =
        string.Empty;

    [Required(ErrorMessage = "Ingrese la calificación.")]
    [Range(0, 100,
        ErrorMessage = "La calificación debe estar entre 0 y 100.")]
    [Display(Name = "Calificación")]
    public int? CalificacionManual { get; set; }

    [Required(ErrorMessage = "Seleccione el resultado.")]
    [RegularExpression("^(Aprobado|Rechazado)$",
        ErrorMessage = "Seleccione Aprobado o Rechazado.")]
    public string Resultado { get; set; } =
        string.Empty;

    [Required(ErrorMessage = "Escriba los comentarios finales.")]
    [StringLength(1000,
        ErrorMessage = "Los comentarios no deben superar 1000 caracteres.")]
    [Display(Name = "Comentarios finales")]
    public string ComentariosFinales { get; set; } =
        string.Empty;
}