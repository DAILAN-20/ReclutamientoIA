using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReclutamientoIA.Data;
using ReclutamientoIA.Models;

namespace ReclutamientoIA.Controllers;

[Authorize(Roles = "Administrador,Reclutador")]
public class EntrevistasController : Controller
{
    private readonly ReclutamientoDbContext _context;

    public EntrevistasController(
        ReclutamientoDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // LISTAR ENTREVISTAS
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var entrevistas = await _context.Entrevistas
            .AsNoTracking()
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Candidato)
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Vacante)
            .OrderBy(e => e.FechaEntrevista)
            .ThenBy(e => e.HoraEntrevista)
            .ToListAsync();

        return View(entrevistas);
    }

    // =====================================================
    // AGENDAR ENTREVISTA - GET
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Agendar(
        int idAnalisis)
    {
        var analisis = await _context.AnalisisIA
            .AsNoTracking()
            .Include(a => a.Candidato)
            .Include(a => a.Vacante)
            .FirstOrDefaultAsync(
                a => a.IdAnalisis == idAnalisis
            );

        if (analisis == null)
        {
            return NotFound();
        }

        if (analisis.EstadoProceso != "Preseleccionado")
        {
            TempData["Error"] =
                "Solo se puede programar una entrevista " +
                "para candidatos preseleccionados.";

            return RedirectToAction(
                "DetalleAnalisis",
                "Vacantes",
                new { idAnalisis }
            );
        }

        var existe = await _context.Entrevistas
            .AsNoTracking()
            .AnyAsync(e =>
                e.IdAnalisis == idAnalisis
            );

        if (existe)
        {
            TempData["Error"] =
                "Este candidato ya tiene una entrevista.";

            return RedirectToAction(
                "DetalleAnalisis",
                "Vacantes",
                new { idAnalisis }
            );
        }

        var modelo = new AgendarEntrevistaViewModel
        {
            IdAnalisis = analisis.IdAnalisis,

            NombreCandidato =
                analisis.Candidato.NombreCompleto ??
                "Nombre no disponible",

            TituloVacante =
                analisis.Vacante.Titulo,

            FechaEntrevista =
                DateTime.Today.AddDays(1),

            HoraEntrevista =
                new TimeSpan(9, 0, 0)
        };

        return View(modelo);
    }

    // =====================================================
    // AGENDAR ENTREVISTA - POST
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Agendar(
        AgendarEntrevistaViewModel modelo)
    {
        var analisis = await _context.AnalisisIA
            .Include(a => a.Candidato)
            .Include(a => a.Vacante)
            .FirstOrDefaultAsync(
                a => a.IdAnalisis == modelo.IdAnalisis
            );

        if (analisis == null)
        {
            return NotFound();
        }

        modelo.NombreCandidato =
            analisis.Candidato.NombreCompleto ??
            "Nombre no disponible";

        modelo.TituloVacante =
            analisis.Vacante.Titulo;

        var fechaHora = modelo.FechaEntrevista.Date
            .Add(modelo.HoraEntrevista);

        if (fechaHora <= DateTime.Now)
        {
            ModelState.AddModelError(
                nameof(modelo.FechaEntrevista),
                "La entrevista debe programarse para una " +
                "fecha y hora futuras."
            );
        }

        if (analisis.EstadoProceso != "Preseleccionado")
        {
            ModelState.AddModelError(
                string.Empty,
                "El candidato no está preseleccionado."
            );
        }

        var existe = await _context.Entrevistas
            .AsNoTracking()
            .AnyAsync(e =>
                e.IdAnalisis == modelo.IdAnalisis
            );

        if (existe)
        {
            ModelState.AddModelError(
                string.Empty,
                "El candidato ya tiene una entrevista."
            );
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var entrevista = new Entrevista
        {
            IdAnalisis = modelo.IdAnalisis,
            FechaEntrevista =
                modelo.FechaEntrevista.Date,
            HoraEntrevista =
                modelo.HoraEntrevista,
            Entrevistador =
                modelo.Entrevistador,
            ComentariosIniciales =
                modelo.ComentariosIniciales,
            EnlaceEntrevista =
                modelo.EnlaceEntrevista,
            EstadoEntrevista = "Pendiente",
            FechaCreacion = DateTime.Now
        };

        _context.Entrevistas.Add(entrevista);

        analisis.EstadoProceso =
            "Entrevista Programada";

        await _context.SaveChangesAsync();

        TempData["Mensaje"] =
            "La entrevista fue programada correctamente.";

        return RedirectToAction(nameof(Index));
    }
    // =====================================================
    // EDITAR ENTREVISTA - GET
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Editar(
        int idEntrevista)
    {
        var entrevista = await _context.Entrevistas
            .AsNoTracking()
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Candidato)
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Vacante)
            .FirstOrDefaultAsync(
                e => e.IdEntrevista == idEntrevista
            );

        if (entrevista == null)
        {
            return NotFound();
        }

        if (entrevista.EstadoEntrevista != "Pendiente")
        {
            TempData["Error"] =
                "Solo se pueden editar entrevistas pendientes.";

            return RedirectToAction(nameof(Index));
        }

        var modelo = new EditarEntrevistaViewModel
        {
            IdEntrevista = entrevista.IdEntrevista,

            NombreCandidato =
                entrevista.AnalisisIA.Candidato
                    .NombreCompleto ??
                "Nombre no disponible",

            TituloVacante =
                entrevista.AnalisisIA.Vacante.Titulo,

            FechaEntrevista =
                entrevista.FechaEntrevista,

            HoraEntrevista =
                entrevista.HoraEntrevista,

            Entrevistador =
                entrevista.Entrevistador,

            ComentariosIniciales =
                entrevista.ComentariosIniciales,

            EnlaceEntrevista =
                entrevista.EnlaceEntrevista
        };

        return View(modelo);
    }

    // =====================================================
    // EDITAR ENTREVISTA - POST
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(
        EditarEntrevistaViewModel modelo)
    {
        var entrevista = await _context.Entrevistas
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Candidato)
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Vacante)
            .FirstOrDefaultAsync(
                e => e.IdEntrevista == modelo.IdEntrevista
            );

        if (entrevista == null)
        {
            return NotFound();
        }

        modelo.NombreCandidato =
            entrevista.AnalisisIA.Candidato
                .NombreCompleto ??
            "Nombre no disponible";

        modelo.TituloVacante =
            entrevista.AnalisisIA.Vacante.Titulo;

        if (entrevista.EstadoEntrevista != "Pendiente")
        {
            ModelState.AddModelError(
                string.Empty,
                "Solo se pueden editar entrevistas pendientes."
            );
        }

        var fechaHora = modelo.FechaEntrevista.Date
            .Add(modelo.HoraEntrevista);

        if (fechaHora <= DateTime.Now)
        {
            ModelState.AddModelError(
                nameof(modelo.FechaEntrevista),
                "La fecha y hora deben ser futuras."
            );
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        entrevista.FechaEntrevista =
            modelo.FechaEntrevista.Date;

        entrevista.HoraEntrevista =
            modelo.HoraEntrevista;

        entrevista.Entrevistador =
            modelo.Entrevistador;

        entrevista.ComentariosIniciales =
            modelo.ComentariosIniciales;

        entrevista.EnlaceEntrevista =
            modelo.EnlaceEntrevista;

        await _context.SaveChangesAsync();

        TempData["Mensaje"] =
            "La entrevista fue actualizada correctamente.";

        return RedirectToAction(nameof(Index));
    }
}