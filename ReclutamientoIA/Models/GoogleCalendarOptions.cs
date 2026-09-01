using System.ComponentModel.DataAnnotations;

namespace ReclutamientoIA.Models;

public class GoogleCalendarOptions
{
    public const string Seccion = "GoogleCalendar";

    [Required]
    public string ClientId { get; set; } =
        string.Empty;

    [Required]
    public string ClientSecret { get; set; } =
        string.Empty;

    [Required]
    public string CalendarId { get; set; } =
        "primary";

    [Required]
    public string ZonaHoraria { get; set; } =
        "America/Tegucigalpa";

    [Range(15, 480)]
    public int DuracionMinutos { get; set; } = 60;
}