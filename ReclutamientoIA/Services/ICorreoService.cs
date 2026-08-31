namespace ReclutamientoIA.Services;

public interface ICorreoService
{
    Task EnviarAsync(
        string destinatario,
        string asunto,
        string contenidoHtml,
        CancellationToken cancellationToken = default
    );
}