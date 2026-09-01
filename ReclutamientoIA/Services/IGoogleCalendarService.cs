using ReclutamientoIA.Models;

namespace ReclutamientoIA.Services;

public interface IGoogleCalendarService
{
    Task<string> CrearEventoAsync(
        Entrevista entrevista,
        AnalisisIA analisis,
        CancellationToken cancellationToken = default
    );

    Task ActualizarEventoAsync(
        string googleEventId,
        Entrevista entrevista,
        AnalisisIA analisis,
        CancellationToken cancellationToken = default
    );

    Task EliminarEventoAsync(
        string googleEventId,
        CancellationToken cancellationToken = default
    );
}