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

    public EntrevistasController(ReclutamientoDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // LISTAR ENTREVISTAS Y FILTRAR
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        int? idAnalisis,
        string? estado,
        DateTime? fecha)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(
                "Los filtros enviados no son válidos."
            );
        }

        estado = string.IsNullOrWhiteSpace(estado)
            ? null
            : estado.Trim();

        if (estado != null &&
            estado != "Pendiente" &&
            estado != "Realizada" &&
            estado != "Cancelada")
        {
            return BadRequest(
                "El estado debe ser Pendiente, Realizada o Cancelada."
            );
        }

        if (idAnalisis.HasValue)
        {
            var existe = await _context.AnalisisIA
                .AsNoTracking()
                .AnyAsync(a =>
                    a.IdAnalisis == idAnalisis.Value
                );

            if (!existe)
            {
                return NotFound();
            }
        }

        IQueryable<Entrevista> consulta = _context.Entrevistas
            .AsNoTracking()
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Candidato)
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Vacante);

        // Conservar el filtro del candidato y su vacante.
        if (idAnalisis.HasValue)
        {
            consulta = consulta.Where(e =>
                e.IdAnalisis == idAnalisis.Value
            );
        }

        if (estado != null)
        {
            consulta = consulta.Where(e =>
                e.EstadoEntrevista == estado
            );
        }

        if (fecha.HasValue)
        {
            var fechaSeleccionada = fecha.Value.Date;

            consulta = consulta.Where(e =>
                e.FechaEntrevista.Date == fechaSeleccionada
            );
        }

        var entrevistas = await consulta
            .OrderBy(e => e.FechaEntrevista)
            .ThenBy(e => e.HoraEntrevista)
            .ThenBy(e => e.IdEntrevista)
            .ToListAsync();

        // Mantener los filtros seleccionados en la vista.
        ViewBag.IdAnalisis = idAnalisis;
        ViewBag.Estado = estado;
        ViewBag.Fecha = fecha?.ToString("yyyy-MM-dd");

        return View(entrevistas);
    }

    // =====================================================
    // AGENDAR ENTREVISTA - GET
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Agendar(int idAnalisis)
    {
        var analisis = await _context.AnalisisIA
            .AsNoTracking()
            .Include(a => a.Candidato)
            .Include(a => a.Vacante)
            .FirstOrDefaultAsync(a =>
                a.IdAnalisis == idAnalisis
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
                e.IdAnalisis == idAnalisis &&
                e.EstadoEntrevista != "Cancelada"
            );

        if (existe)
        {
            TempData["Error"] =
                "Este candidato ya tiene una entrevista " +
                "pendiente o realizada.";

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
            TituloVacante = analisis.Vacante.Titulo,
            FechaEntrevista = DateTime.Today.AddDays(1),
            HoraEntrevista = new TimeSpan(9, 0, 0)
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
            .FirstOrDefaultAsync(a =>
                a.IdAnalisis == modelo.IdAnalisis
            );

        if (analisis == null)
        {
            return NotFound();
        }

        modelo.NombreCandidato =
            analisis.Candidato.NombreCompleto ??
            "Nombre no disponible";

        modelo.TituloVacante = analisis.Vacante.Titulo;

        ValidarFechaHora(
            modelo.FechaEntrevista,
            modelo.HoraEntrevista
        );

        if (analisis.EstadoProceso != "Preseleccionado")
        {
            ModelState.AddModelError(
                string.Empty,
                "El candidato no está preseleccionado."
            );
        }

        var entrevista = await _context.Entrevistas
            .FirstOrDefaultAsync(e =>
                e.IdAnalisis == modelo.IdAnalisis
            );

        if (entrevista != null &&
            entrevista.EstadoEntrevista != "Cancelada")
        {
            ModelState.AddModelError(
                string.Empty,
                "El candidato ya tiene una entrevista " +
                "pendiente o realizada."
            );
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        // Reutilizar el registro si estaba cancelado.
        if (entrevista == null)
        {
            entrevista = new Entrevista
            {
                IdAnalisis = modelo.IdAnalisis,
                FechaCreacion = DateTime.Now
            };

            _context.Entrevistas.Add(entrevista);
        }

        entrevista.FechaEntrevista = modelo.FechaEntrevista.Date;
        entrevista.HoraEntrevista = modelo.HoraEntrevista;
        entrevista.Entrevistador = modelo.Entrevistador;
        entrevista.ComentariosIniciales = modelo.ComentariosIniciales;
        entrevista.EnlaceEntrevista = modelo.EnlaceEntrevista;
        entrevista.ComentariosFinales = null;
        entrevista.CalificacionManual = null;
        entrevista.Resultado = null;
        entrevista.EstadoEntrevista = "Pendiente";

        analisis.EstadoProceso = "Entrevista Programada";

        await _context.SaveChangesAsync();

        TempData["Mensaje"] =
            "La entrevista fue programada correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // EDITAR ENTREVISTA - GET
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Editar(int idEntrevista)
    {
        var entrevista = await _context.Entrevistas
            .AsNoTracking()
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Candidato)
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Vacante)
            .FirstOrDefaultAsync(e =>
                e.IdEntrevista == idEntrevista
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
                entrevista.AnalisisIA.Candidato.NombreCompleto ??
                "Nombre no disponible",
            TituloVacante = entrevista.AnalisisIA.Vacante.Titulo,
            FechaEntrevista = entrevista.FechaEntrevista,
            HoraEntrevista = entrevista.HoraEntrevista,
            Entrevistador = entrevista.Entrevistador,
            ComentariosIniciales = entrevista.ComentariosIniciales,
            EnlaceEntrevista = entrevista.EnlaceEntrevista
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
            .FirstOrDefaultAsync(e =>
                e.IdEntrevista == modelo.IdEntrevista
            );

        if (entrevista == null)
        {
            return NotFound();
        }

        modelo.NombreCandidato =
            entrevista.AnalisisIA.Candidato.NombreCompleto ??
            "Nombre no disponible";

        modelo.TituloVacante =
            entrevista.AnalisisIA.Vacante.Titulo;

        if (entrevista.EstadoEntrevista != "Pendiente")
        {
            TempData["Error"] =
                "Solo se pueden editar entrevistas pendientes.";

            return RedirectToAction(nameof(Index));
        }

        ValidarFechaHora(
            modelo.FechaEntrevista,
            modelo.HoraEntrevista
        );

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        entrevista.FechaEntrevista = modelo.FechaEntrevista.Date;
        entrevista.HoraEntrevista = modelo.HoraEntrevista;
        entrevista.Entrevistador = modelo.Entrevistador;
        entrevista.ComentariosIniciales = modelo.ComentariosIniciales;
        entrevista.EnlaceEntrevista = modelo.EnlaceEntrevista;

        await _context.SaveChangesAsync();

        TempData["Mensaje"] =
            "La entrevista fue actualizada correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // CANCELAR ENTREVISTA - POST
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int idEntrevista)
    {
        var entrevista = await _context.Entrevistas
            .Include(e => e.AnalisisIA)
            .FirstOrDefaultAsync(e =>
                e.IdEntrevista == idEntrevista
            );

        if (entrevista == null)
        {
            return NotFound();
        }

        if (entrevista.EstadoEntrevista != "Pendiente")
        {
            TempData["Error"] =
                "Solo se pueden cancelar entrevistas pendientes.";

            return RedirectToAction(nameof(Index));
        }

        entrevista.EstadoEntrevista = "Cancelada";
        entrevista.Resultado = null;
        entrevista.AnalisisIA.EstadoProceso = "Preseleccionado";

        await _context.SaveChangesAsync();

        TempData["Mensaje"] =
            "La entrevista fue cancelada. " +
            "El candidato quedó preseleccionado y puede " +
            "programarse nuevamente.";

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // CALIFICAR ENTREVISTA - GET
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Calificar(int idEntrevista)
    {
        var entrevista = await _context.Entrevistas
            .AsNoTracking()
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Candidato)
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Vacante)
            .FirstOrDefaultAsync(e =>
                e.IdEntrevista == idEntrevista
            );

        if (entrevista == null)
        {
            return NotFound();
        }

        if (entrevista.EstadoEntrevista != "Pendiente" ||
            entrevista.AnalisisIA.EstadoProceso != "Entrevista Programada")
        {
            TempData["Error"] =
                "Solo se pueden calificar entrevistas pendientes " +
                "de candidatos con entrevista programada.";

            return RedirectToAction(nameof(Index));
        }

        var fechaHora = entrevista.FechaEntrevista.Date
            .Add(entrevista.HoraEntrevista);

        if (fechaHora > DateTime.Now)
        {
            TempData["Error"] =
                "No se puede calificar una entrevista " +
                "antes de su fecha y hora programadas.";

            return RedirectToAction(nameof(Index));
        }

        var modelo = new CalificarEntrevistaViewModel
        {
            IdEntrevista = entrevista.IdEntrevista,
            NombreCandidato =
                entrevista.AnalisisIA.Candidato.NombreCompleto ??
                "Nombre no disponible",
            TituloVacante = entrevista.AnalisisIA.Vacante.Titulo
        };

        return View(modelo);
    }

    // =====================================================
    // CALIFICAR ENTREVISTA - POST
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calificar(
        CalificarEntrevistaViewModel modelo)
    {
        var entrevista = await _context.Entrevistas
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Candidato)
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Vacante)
            .FirstOrDefaultAsync(e =>
                e.IdEntrevista == modelo.IdEntrevista
            );

        if (entrevista == null)
        {
            return NotFound();
        }

        if (entrevista.EstadoEntrevista != "Pendiente" ||
            entrevista.AnalisisIA.EstadoProceso != "Entrevista Programada")
        {
            TempData["Error"] =
                "Solo se pueden calificar entrevistas pendientes " +
                "de candidatos con entrevista programada.";

            return RedirectToAction(nameof(Index));
        }

        var fechaHora = entrevista.FechaEntrevista.Date
            .Add(entrevista.HoraEntrevista);

        if (fechaHora > DateTime.Now)
        {
            TempData["Error"] =
                "No se puede calificar una entrevista " +
                "antes de su fecha y hora programadas.";

            return RedirectToAction(nameof(Index));
        }

        modelo.NombreCandidato =
            entrevista.AnalisisIA.Candidato.NombreCompleto ??
            "Nombre no disponible";

        modelo.TituloVacante =
            entrevista.AnalisisIA.Vacante.Titulo;

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        entrevista.CalificacionManual = modelo.CalificacionManual;
        entrevista.ComentariosFinales = modelo.ComentariosFinales.Trim();
        entrevista.Resultado = modelo.Resultado;
        entrevista.EstadoEntrevista = "Realizada";
        entrevista.AnalisisIA.EstadoProceso = modelo.Resultado;

        await _context.SaveChangesAsync();

        TempData["Mensaje"] =
            "La entrevista fue calificada correctamente. " +
            "Resultado: " + modelo.Resultado + ".";

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // VER EVALUACIÓN DE ENTREVISTA
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Detalle(int idEntrevista)
    {
        var entrevista = await _context.Entrevistas
            .AsNoTracking()
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Candidato)
            .Include(e => e.AnalisisIA)
                .ThenInclude(a => a.Vacante)
            .FirstOrDefaultAsync(e =>
                e.IdEntrevista == idEntrevista
            );

        if (entrevista == null)
        {
            return NotFound();
        }

        if (entrevista.EstadoEntrevista != "Realizada")
        {
            TempData["Error"] =
                "La entrevista todavía no tiene una evaluación final.";

            return RedirectToAction(
                nameof(Index),
                new { idAnalisis = entrevista.IdAnalisis }
            );
        }

        return View(entrevista);
    }

    // =====================================================
    // VALIDAR FECHA Y HORA
    // =====================================================

    private void ValidarFechaHora(DateTime fecha, TimeSpan hora)
    {
        if (hora < TimeSpan.Zero ||
            hora >= TimeSpan.FromDays(1))
        {
            ModelState.AddModelError(
                "HoraEntrevista",
                "Seleccione una hora válida entre 00:00 y 23:59."
            );

            return;
        }

        var fechaHora = fecha.Date.Add(hora);

        if (fechaHora <= DateTime.Now)
        {
            ModelState.AddModelError(
                "FechaEntrevista",
                "La entrevista debe programarse para una " +
                "fecha y hora futuras."
            );
        }
    }
}