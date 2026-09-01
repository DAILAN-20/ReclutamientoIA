using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReclutamientoIA.Data;
using ReclutamientoIA.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ReclutamientoIA.Controllers;

[Authorize(Roles = "Administrador,Reclutador")]
public class CorreoCurriculumsController : Controller
{
    private readonly ReclutamientoDbContext _context;

    private readonly ICorreoCurriculumsService
        _correoCurriculumsService;

    private readonly ILogger<CorreoCurriculumsController>
        _logger;

    public CorreoCurriculumsController(
        ReclutamientoDbContext context,
        ICorreoCurriculumsService correoCurriculumsService,
        ILogger<CorreoCurriculumsController> logger)
    {
        _context = context;

        _correoCurriculumsService =
            correoCurriculumsService;

        _logger = logger;
    }

    // =====================================================
    // CURRÍCULUMS RECIBIDOS POR CORREO
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? buscar)
    {
        buscar = string.IsNullOrWhiteSpace(buscar)
            ? null
            : buscar.Trim();

        var consulta = _context.Candidatos
            .AsNoTracking()
            .Where(c =>
    c.OrigenRegistro != null &&
    c.OrigenRegistro.Contains("Correo")
);

        if (buscar != null)
        {
            consulta = consulta.Where(c =>
                (c.NombreCompleto != null &&
                 c.NombreCompleto.Contains(buscar)) ||

                (c.Correo != null &&
                 c.Correo.Contains(buscar)) ||

                (c.AsuntoCorreo != null &&
                 c.AsuntoCorreo.Contains(buscar)) ||

                c.NombreArchivoOriginal.Contains(buscar)
            );
        }

        var candidatos = await consulta
            .OrderByDescending(c => c.FechaCorreo)
            .ThenByDescending(c => c.FechaRegistro)
            .ToListAsync();
        var idsCurriculumsCorreo =
    await _context.Candidatos
        .AsNoTracking()
        .Where(c =>
            c.Activo &&
            c.OrigenRegistro != null &&
            c.OrigenRegistro.Contains("Correo")
        )
        .Select(c => c.IdCandidato)
        .ToListAsync();

        var vacantes = await _context.Vacantes
            .AsNoTracking()
            .Where(v =>
                v.Estado == "Activa" ||
                v.Estado == "Activo"
            )
            .OrderBy(v => v.Titulo)
            .ToListAsync();

        ViewBag.IdsCurriculumsCorreo =
            idsCurriculumsCorreo;

        ViewBag.Vacantes = new SelectList(
            vacantes,
            nameof(ReclutamientoIA.Models.Vacante.IdVacante),
            nameof(ReclutamientoIA.Models.Vacante.Titulo)
        );

        ViewBag.Buscar = buscar;

        return View(candidatos);
    }

    // =====================================================
    // REVISAR CORREO E IMPORTAR PDF
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Importar()
    {
        try
        {
            using var limite =
                new CancellationTokenSource(
                    TimeSpan.FromMinutes(10)
                );

            var resultado =
                await _correoCurriculumsService
                    .ImportarAsync(limite.Token);

            TempData["Mensaje"] =
                $"Correos revisados: " +
                $"{resultado.CorreosRevisados}. " +

                $"PDF encontrados: " +
                $"{resultado.PdfEncontrados}. " +

                $"Currículums importados: " +
                $"{resultado.Importados}. " +

                $"Duplicados: " +
                $"{resultado.Duplicados}.";

            if (resultado.Errores > 0)
            {
                var detalles = resultado
                    .DetallesErrores
                    .Take(5)
                    .ToList();

                TempData["Error"] =
                    $"{resultado.Errores} archivo(s) " +
                    "presentaron errores. " +
                    string.Join(" ", detalles);
            }
        }
        catch (OperationCanceledException)
        {
            TempData["Error"] =
                "La revisión del correo tardó demasiado. " +
                "Intente nuevamente.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "No se pudo revisar el correo. " +
                "Tipo de error: {TipoError}",
                ex.GetType().Name
            );

            TempData["Error"] =
                "No fue posible conectarse al correo. " +
                "Revise el servidor IMAP, el usuario y " +
                "la contraseña de aplicación.";
        }

        return RedirectToAction(nameof(Index));
    }
}