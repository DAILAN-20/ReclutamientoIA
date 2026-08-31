using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReclutamientoIA.Data;
using ReclutamientoIA.Models;
using ReclutamientoIA.Services;

namespace ReclutamientoIA.Controllers;

[Authorize(Roles = "Administrador,Reclutador")]
public class VacantesController : Controller
{
    private readonly ReclutamientoDbContext _context;
    private readonly ICurriculumService _curriculumService;
    private readonly IPdfTextoService _pdfTextoService;

    private readonly IInteligenciaArtificialService
        _inteligenciaArtificialService;

    public VacantesController(
        ReclutamientoDbContext context,
        ICurriculumService curriculumService,
        IPdfTextoService pdfTextoService,
        IInteligenciaArtificialService inteligenciaArtificialService)
    {
        _context = context;
        _curriculumService = curriculumService;
        _pdfTextoService = pdfTextoService;
        _inteligenciaArtificialService =
            inteligenciaArtificialService;
    }

    // =====================================================
    // LISTAR VACANTES
    // =====================================================

    public async Task<IActionResult> Index()
    {
        var vacantes = await _context.Vacantes
            .AsNoTracking()
            .OrderByDescending(v => v.FechaCreacion)
            .ToListAsync();

        return View(vacantes);
    }

    // =====================================================
    // DETALLES DE VACANTE
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Details(int? idVacante)
    {
        if (idVacante == null)
        {
            return NotFound();
        }

        var vacante = await _context.Vacantes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                v => v.IdVacante == idVacante
            );

        if (vacante == null)
        {
            return NotFound();
        }

        return View(vacante);
    }

    // =====================================================
    // CREAR VACANTE
    // =====================================================

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(
            "Titulo,Descripcion,Requisitos,Estado"
        )]
        Vacante vacante)
    {
        if (!ModelState.IsValid)
        {
            return View(vacante);
        }

        vacante.FechaCreacion = DateTime.Now;

        _context.Vacantes.Add(vacante);
        await _context.SaveChangesAsync();

        TempData["Mensaje"] =
            "La vacante fue creada correctamente.";

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // EDITAR VACANTE
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Edit(int? idVacante)
    {
        if (idVacante == null)
        {
            return NotFound();
        }

        var vacante = await _context.Vacantes
            .FindAsync(idVacante);

        if (vacante == null)
        {
            return NotFound();
        }

        return View(vacante);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int idVacante,
        [Bind(
            "IdVacante,Titulo,Descripcion," +
            "Requisitos,Estado,FechaCreacion"
        )]
        Vacante vacante)
    {
        if (idVacante != vacante.IdVacante)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(vacante);
        }

        try
        {
            _context.Update(vacante);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] =
                "La vacante fue actualizada correctamente.";
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!VacanteExists(vacante.IdVacante))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // ELIMINAR VACANTE
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Delete(int? idVacante)
    {
        if (idVacante == null)
        {
            return NotFound();
        }

        var vacante = await _context.Vacantes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                v => v.IdVacante == idVacante
            );

        if (vacante == null)
        {
            return NotFound();
        }

        return View(vacante);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        int idVacante)
    {
        var vacante = await _context.Vacantes
            .FindAsync(idVacante);

        if (vacante != null)
        {
            _context.Vacantes.Remove(vacante);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] =
                "La vacante fue eliminada.";
        }

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // SELECCIONAR CANDIDATOS - GET
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> SeleccionarCandidatos(
        int idVacante)
    {
        var vacante = await _context.Vacantes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                v => v.IdVacante == idVacante
            );

        if (vacante == null)
        {
            return NotFound();
        }

        var candidatosYaAnalizados =
            await _context.AnalisisIA
                .Where(a => a.IdVacante == idVacante)
                .Select(a => a.IdCandidato)
                .ToListAsync();

        var candidatosDisponibles =
            await _context.Candidatos
                .AsNoTracking()
                .Where(c =>
                    c.Activo &&
                    !candidatosYaAnalizados.Contains(
                        c.IdCandidato
                    )
                )
                .OrderByDescending(c => c.FechaRegistro)
                .Select(c =>
                    new CandidatoParaAnalisisViewModel
                    {
                        IdCandidato = c.IdCandidato,

                        NombreCompleto =
                            c.NombreCompleto ??
                            "Pendiente de extracción",

                        Correo =
                            c.Correo ?? "No disponible",

                        NombreArchivo =
                            c.NombreArchivoOriginal,

                        FechaRegistro =
                            c.FechaRegistro
                    }
                )
                .ToListAsync();

        var modelo = new SeleccionarCandidatosViewModel
        {
            IdVacante = vacante.IdVacante,
            TituloVacante = vacante.Titulo,
            Candidatos = candidatosDisponibles
        };

        if (TempData["Error"] != null)
        {
            ModelState.AddModelError(
                string.Empty,
                TempData["Error"]!.ToString()!
            );
        }

        return View(modelo);
    }

    // =====================================================
    // ANALIZAR CANDIDATOS - POST
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeleccionarCandidatos(
        SeleccionarCandidatosViewModel modelo)
    {
        if (modelo.CandidatosSeleccionados == null ||
            !modelo.CandidatosSeleccionados.Any())
        {
            TempData["Error"] =
                "Seleccione al menos un candidato.";

            return RedirectToAction(
                nameof(SeleccionarCandidatos),
                new { idVacante = modelo.IdVacante }
            );
        }

        var vacante = await _context.Vacantes
            .FirstOrDefaultAsync(
                v => v.IdVacante == modelo.IdVacante
            );

        if (vacante == null)
        {
            return NotFound();
        }

        var configuracion =
            await _context.ConfiguracionesIA
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.Activo &&
                         c.Proveedor == "OpenAI"
                );

        var prompt = await _context.PromptsIA
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Activo &&
                     p.Tipo == "AnalisisCV"
            );

        if (configuracion == null || prompt == null)
        {
            TempData["Error"] =
                "La configuración de inteligencia artificial " +
                "no está activa.";

            return RedirectToAction(
                nameof(SeleccionarCandidatos),
                new { idVacante = modelo.IdVacante }
            );
        }

        var identificadores = modelo.CandidatosSeleccionados
            .Distinct()
            .ToList();

        var procesados = 0;
        var errores = new List<string>();

        foreach (var idCandidato in identificadores)
        {
            var yaExiste = await _context.AnalisisIA
                .AsNoTracking()
                .AnyAsync(a =>
                    a.IdVacante == vacante.IdVacante &&
                    a.IdCandidato == idCandidato
                );

            if (yaExiste)
            {
                continue;
            }

            try
            {
                var candidato = await _context.Candidatos
                    .FirstOrDefaultAsync(c =>
                        c.IdCandidato == idCandidato &&
                        c.Activo
                    );

                if (candidato == null)
                {
                    continue;
                }

                using var archivo =
                    _curriculumService.AbrirLectura(
                        candidato.NombreArchivoGuardado
                    );

                if (archivo == null)
                {
                    throw new InvalidOperationException(
                        "No se encontró el currículum."
                    );
                }

                var textoCurriculum =
                    _pdfTextoService.ExtraerTexto(archivo);

                var resultado =
                    await _inteligenciaArtificialService
                        .AnalizarAsync(
                            vacante,
                            textoCurriculum,
                            configuracion,
                            prompt
                        );

                if (!string.IsNullOrWhiteSpace(
                        resultado.Nombre))
                {
                    candidato.NombreCompleto =
                        resultado.Nombre;
                }

                if (!string.IsNullOrWhiteSpace(
                        resultado.Correo))
                {
                    candidato.Correo =
                        resultado.Correo;
                }

                if (!string.IsNullOrWhiteSpace(
                        resultado.Telefono))
                {
                    candidato.Telefono =
                        resultado.Telefono;
                }

                var analisis = new AnalisisIA
                {
                    IdVacante = vacante.IdVacante,

                    IdCandidato =
                        candidato.IdCandidato,

                    Puntuacion = Math.Clamp(
                        resultado.Puntuacion,
                        0,
                        100
                    ),

                    Resumen = resultado.Resumen,

                    Fortalezas =
                        resultado.Fortalezas,

                    Debilidades =
                        resultado.Debilidades,

                    Recomendacion =
                        resultado.Recomendacion,

                    EstadoProceso = "Analizado",

                    ProveedorIA =
                        configuracion.Proveedor,

                    ModeloIA =
                        configuracion.Modelo,

                    FechaAnalisis = DateTime.Now
                };

                _context.AnalisisIA.Add(analisis);

                await _context.SaveChangesAsync();

                procesados++;
            }
            catch (Exception ex)
            {
                var detalle =
                    ex.InnerException?.Message ??
                    ex.Message;

                errores.Add(
                    $"No se pudo analizar el candidato " +
                    $"con identificador {idCandidato}. " +
                    $"Detalle SQL: {detalle}"
                );

                _context.ChangeTracker.Clear();
            }
        }

        if (procesados > 0)
        {
            TempData["Mensaje"] =
                $"{procesados} candidato(s) fueron " +
                "analizados correctamente.";
        }

        if (errores.Any())
        {
            TempData["Error"] =
                string.Join(" ", errores);
        }

        return RedirectToAction(
            nameof(Resultados),
            new { idVacante = vacante.IdVacante }
        );
    }

    // =====================================================
    // RESULTADOS Y RANKING
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Resultados(
        int idVacante)
    {
        var vacante = await _context.Vacantes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                v => v.IdVacante == idVacante
            );

        if (vacante == null)
        {
            return NotFound();
        }

        var resultados = await _context.AnalisisIA
            .AsNoTracking()
            .Include(a => a.Candidato)
            .Where(a => a.IdVacante == idVacante)
            .OrderByDescending(a => a.Puntuacion)
            .ToListAsync();

        ViewBag.Vacante = vacante;

        return View(resultados);
    }


    [HttpGet]
    public async Task<IActionResult> DetalleAnalisis(
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

        return View(analisis);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preseleccionar(
    int idAnalisis)
    {
        var analisis = await _context.AnalisisIA
            .FirstOrDefaultAsync(
                a => a.IdAnalisis == idAnalisis
            );

        if (analisis == null)
        {
            return NotFound();
        }

        if (analisis.EstadoProceso != "Analizado")
        {
            TempData["Error"] =
                "El candidato ya fue procesado anteriormente.";

            return RedirectToAction(
                nameof(DetalleAnalisis),
                new { idAnalisis }
            );
        }

        analisis.EstadoProceso = "Preseleccionado";

        await _context.SaveChangesAsync();

        TempData["Mensaje"] =
            "El candidato fue preseleccionado correctamente.";

        return RedirectToAction(
            nameof(DetalleAnalisis),
            new { idAnalisis }
        );
    }

    // =====================================================
    // TABLERO DE CANDIDATOS
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Tablero(int? idVacante)
    {
        if (idVacante.HasValue)
        {
            var existe = await _context.Vacantes
                .AsNoTracking()
                .AnyAsync(v => v.IdVacante == idVacante.Value);

            if (!existe)
            {
                return NotFound();
            }
        }

        var consulta = _context.AnalisisIA
            .AsNoTracking()
            .Include(a => a.Candidato)
            .Include(a => a.Vacante)
            .Where(a =>
                a.EstadoProceso == "Preseleccionado" ||
                a.EstadoProceso == "Entrevista Programada" ||
                a.EstadoProceso == "Aprobado" ||
                a.EstadoProceso == "Rechazado"
            );

        if (idVacante.HasValue)
        {
            consulta = consulta.Where(a =>
                a.IdVacante == idVacante.Value
            );
        }

        var candidatos = await consulta
            .OrderBy(a => a.Candidato.NombreCompleto)
            .ThenBy(a => a.IdAnalisis)
            .ToListAsync();

        ViewBag.Vacantes = await _context.Vacantes
            .AsNoTracking()
            .OrderBy(v => v.Titulo)
            .ToListAsync();

        ViewBag.IdVacante = idVacante;

        // Consultar únicamente las entrevistas pendientes
        // de los candidatos incluidos en el tablero.
        var idsAnalisis = candidatos
            .Select(a => a.IdAnalisis)
            .ToList();

        var entrevistasPendientes = await _context.Entrevistas
            .AsNoTracking()
            .Where(e =>
                idsAnalisis.Contains(e.IdAnalisis) &&
                e.EstadoEntrevista == "Pendiente"
            )
            .ToDictionaryAsync(e => e.IdAnalisis);

        ViewBag.EntrevistasPendientes = entrevistasPendientes;

        return View(candidatos);
    }

    // =====================================================
    // COMPROBAR EXISTENCIA
    // =====================================================

    private bool VacanteExists(int idVacante)
    {
        return _context.Vacantes.Any(
            v => v.IdVacante == idVacante
        );
    }
}