using ReclutamientoIA.Models;

namespace ReclutamientoIA.Services;

public interface ICorreoCurriculumsService
{
    Task<ResultadoImportacionCorreo> ImportarAsync(
        CancellationToken cancellationToken = default
    );
}