using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReclutamientoIA.Data;
using ReclutamientoIA.Models;
using ReclutamientoIA.Services;
using ClosedXML.Excel; // <-- AGREGAR ESTE USING
using System.Text; //

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
    // CARGA MASIVA DE CURRÍCULUMS - GET
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> CargaMasiva(
        int? idVacante)
    {
        await CargarVacantesDisponiblesAsync(idVacante);

        var modelo = new CargaMasivaCurriculumsViewModel
        {
            IdVacante = idVacante ?? 0
        };

        return View(modelo);
    }

    // =====================================================
    // CARGA MASIVA DE CURRÍCULUMS - POST
    // =====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(525_000_000)]
    public async Task<IActionResult> CargaMasiva(
        CargaMasivaCurriculumsViewModel modelo)
    {
        if (modelo.Archivos == null ||
            modelo.Archivos.Count == 0)
        {
            ModelState.AddModelError(
                nameof(modelo.Archivos),
                "Seleccione al menos un currículum."
            );
        }

        const int limiteCurriculums = 50;

        if (modelo.Archivos != null &&
            modelo.Archivos.Count > limiteCurriculums)
        {
            ModelState.AddModelError(
                nameof(modelo.Archivos),
                $"Puede analizar un máximo de {limiteCurriculums} " +
                "currículums por lote."
            );
        }

        var vacante = await _context.Vacantes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                v => v.IdVacante == modelo.IdVacante
            );

        if (vacante == null)
        {
            ModelState.AddModelError(
                nameof(modelo.IdVacante),
                "La vacante seleccionada no existe."
            );
        }

        if (!ModelState.IsValid)
        {
            await CargarVacantesDisponiblesAsync(
                modelo.IdVacante
            );

            return View(modelo);
        }

        var configuracion = await _context.ConfiguracionesIA
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.IdConfiguracion)
            .FirstOrDefaultAsync();

        var prompt = await _context.PromptsIA
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Activo &&
                p.Tipo == "AnalisisCV"
            );

        if (configuracion == null)
        {
            TempData["Error"] =
                "No existe una configuración de inteligencia " +
                "artificial activa.";

            return RedirectToAction(
                nameof(CargaMasiva),
                new { idVacante = modelo.IdVacante }
            );
        }

        if (string.IsNullOrWhiteSpace(
                configuracion.ClaveApiProtegida))
        {
            TempData["Error"] =
                "Primero debe guardar una clave API desde " +
                "Configuración de IA.";

            return RedirectToAction(
                nameof(CargaMasiva),
                new { idVacante = modelo.IdVacante }
            );
        }

        if (prompt == null ||
            string.IsNullOrWhiteSpace(prompt.Contenido))
        {
            TempData["Error"] =
                "No existe un prompt de análisis activo.";

            return RedirectToAction(
                nameof(CargaMasiva),
                new { idVacante = modelo.IdVacante }
            );
        }

        var procesados = 0;
        var yaAnalizados = 0;
        var errores = new List<string>();

        foreach (var archivo in modelo.Archivos)
        {
            var nombreArchivo = Path.GetFileName(
                archivo.FileName
            );

            if (!_curriculumService.EsPdfValido(archivo))
            {
                errores.Add(
                    $"{nombreArchivo}: debe ser un PDF válido " +
                    "y no superar 10 MB."
                );

                continue;
            }

            try
            {
                var hash = await _curriculumService
                    .CalcularHashAsync(archivo);

                var candidato = await _context.Candidatos
                    .FirstOrDefaultAsync(c =>
                        c.HashArchivo == hash
                    );

                if (candidato == null)
                {
                    var nombreGuardado =
                        await _curriculumService.GuardarAsync(
                            archivo
                        );

                    candidato = new Candidato
                    {
                        NombreArchivoOriginal = nombreArchivo,
                        NombreArchivoGuardado = nombreGuardado,
                        TipoContenido = "application/pdf",
                        TamanoArchivo = archivo.Length,
                        HashArchivo = hash,
                        FechaRegistro = DateTime.Now,
                        Activo = true
                    };

                    try
                    {
                        _context.Candidatos.Add(candidato);
                        await _context.SaveChangesAsync();
                    }
                    catch
                    {
                        _curriculumService.Eliminar(
                            nombreGuardado
                        );

                        throw;
                    }
                }

                var analisisExistente =
                    await _context.AnalisisIA
                        .AsNoTracking()
                        .AnyAsync(a =>
                            a.IdVacante == vacante!.IdVacante &&
                            a.IdCandidato == candidato.IdCandidato
                        );

                if (analisisExistente)
                {
                    yaAnalizados++;
                    continue;
                }

                using var lectura =
                    _curriculumService.AbrirLectura(
                        candidato.NombreArchivoGuardado
                    );

                if (lectura == null)
                {
                    throw new InvalidOperationException(
                        "No se encontró el archivo guardado."
                    );
                }

                var textoCurriculum =
                    _pdfTextoService.ExtraerTexto(lectura);

                if (string.IsNullOrWhiteSpace(
                    textoCurriculum
                ))
                {
                    throw new InvalidOperationException(
                        "No se pudo extraer texto del PDF."
                    );
                }

                var resultado =
                    await _inteligenciaArtificialService
                        .AnalizarAsync(
                            vacante!,
                            textoCurriculum,
                            configuracion,
                            prompt
                        );

                if (!string.IsNullOrWhiteSpace(
                    resultado.Nombre
                ))
                {
                    candidato.NombreCompleto =
                        resultado.Nombre.Trim();
                }

                if (!string.IsNullOrWhiteSpace(
                    resultado.Correo
                ))
                {
                    candidato.Correo =
                        resultado.Correo.Trim();
                }

                if (!string.IsNullOrWhiteSpace(
                    resultado.Telefono
                ))
                {
                    candidato.Telefono =
                        resultado.Telefono.Trim();
                }

                var analisis = new AnalisisIA
                {
                    IdVacante = vacante!.IdVacante,
                    IdCandidato = candidato.IdCandidato,
                    Puntuacion = Math.Clamp(
                        resultado.Puntuacion,
                        0,
                        100
                    ),
                    Resumen = resultado.Resumen,
                    Fortalezas = resultado.Fortalezas,
                    Debilidades = resultado.Debilidades,
                    Recomendacion = resultado.Recomendacion,
                    EstadoProceso = "Analizado",
                    ProveedorIA = configuracion.Proveedor,
                    ModeloIA = configuracion.Modelo,
                    FechaAnalisis = DateTime.Now
                };

                _context.AnalisisIA.Add(analisis);
                await _context.SaveChangesAsync();

                procesados++;
            }
            catch
            {
                errores.Add(
                    $"{nombreArchivo}: no fue posible analizarlo."
                );

                _context.ChangeTracker.Clear();
            }
        }

        var mensajes = new List<string>();

        if (procesados > 0)
        {
            mensajes.Add(
                $"{procesados} currículum(s) fueron " +
                "analizados correctamente."
            );
        }

        if (yaAnalizados > 0)
        {
            mensajes.Add(
                $"{yaAnalizados} currículum(s) ya estaban " +
                "analizados para esta vacante."
            );
        }

        if (mensajes.Any())
        {
            TempData["Mensaje"] = string.Join(
                " ",
                mensajes
            );
        }

        if (errores.Any())
        {
            TempData["Error"] = string.Join(
                " ",
                errores.Take(5)
            );

            if (errores.Count > 5)
            {
                TempData["Error"] +=
                    $" Además, {errores.Count - 5} archivo(s) " +
                    "adicionales presentaron errores.";
            }
        }

        return RedirectToAction(
            nameof(Resultados),
            new { idVacante = vacante!.IdVacante }
        );
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
                .Where(c => c.Activo)
                .OrderBy(c => c.IdConfiguracion)
                .FirstOrDefaultAsync();

        var prompt = await _context.PromptsIA
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Activo &&
                     p.Tipo == "AnalisisCV"
            );

        if (configuracion == null)
        {
            TempData["Error"] =
                "No existe una configuración de inteligencia " +
                "artificial activa.";

            return RedirectToAction(
                nameof(SeleccionarCandidatos),
                new { idVacante = modelo.IdVacante }
            );
        }

        if (string.IsNullOrWhiteSpace(
                configuracion.ClaveApiProtegida))
        {
            TempData["Error"] =
                "Primero debe guardar una clave API desde " +
                "Configuración de IA.";

            return RedirectToAction(
                nameof(SeleccionarCandidatos),
                new { idVacante = modelo.IdVacante }
            );
        }

        if (prompt == null ||
            string.IsNullOrWhiteSpace(prompt.Contenido))
        {
            TempData["Error"] =
                "No existe un prompt de análisis activo.";

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

    private async Task CargarVacantesDisponiblesAsync(
        int? idVacante)
    {
        var vacantes = await _context.Vacantes
            .AsNoTracking()
            .Where(v =>
                v.Estado == "Activa" ||
                v.Estado == "Activo"
            )
            .OrderBy(v => v.Titulo)
            .ToListAsync();

        ViewBag.Vacantes = new SelectList(
            vacantes,
            nameof(Vacante.IdVacante),
            nameof(Vacante.Titulo),
            idVacante
        );
    }

    private bool VacanteExists(int idVacante)
    {
        return _context.Vacantes.Any(
            v => v.IdVacante == idVacante
        );
    }
    // =====================================================
    // EXPORTAR RESULTADOS A EXCEL
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> ExportarResultados(int idVacante)
    {
        try
        {
            var vacante = await _context.Vacantes
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.IdVacante == idVacante);

            if (vacante == null)
            {
                TempData["Error"] = "La vacante no existe.";
                return RedirectToAction(nameof(Index));
            }

            var resultados = await _context.AnalisisIA
                .AsNoTracking()
                .Include(a => a.Candidato)
                .Where(a => a.IdVacante == idVacante)
                .OrderByDescending(a => a.Puntuacion)
                .ToListAsync();

            if (!resultados.Any())
            {
                TempData["Error"] = "No hay resultados para exportar.";
                return RedirectToAction(nameof(Resultados), new { idVacante });
            }

            // Construir el archivo Excel
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Resultados");

                // ESTILOS
                var headerStyle = workbook.Style;
                headerStyle.Font.Bold = true;
                headerStyle.Font.FontColor = XLColor.White;
                headerStyle.Fill.BackgroundColor = XLColor.FromArgb(52, 73, 94);
                headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerStyle.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerStyle.Border.InsideBorder = XLBorderStyleValues.Thin;

                // ENCABEZADOS
                string[] headers = {
                "Puesto",
                "Candidato",
                "Correo",
                "Teléfono",
                "Puntuación",
                "Resumen",
                "Fortalezas",
                "Debilidades",
                "Recomendación",
                "Estado",
                "Fecha Análisis",
                "Proveedor IA",
                "Modelo IA"
            };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style = headerStyle;
                }

                // DATOS
                int row = 2;
                foreach (var item in resultados)
                {
                    worksheet.Cell(row, 1).Value = vacante.Titulo;
                    worksheet.Cell(row, 2).Value = item.Candidato?.NombreCompleto ?? "No disponible";
                    worksheet.Cell(row, 3).Value = item.Candidato?.Correo ?? "No disponible";
                    worksheet.Cell(row, 4).Value = item.Candidato?.Telefono ?? "No disponible";
                    worksheet.Cell(row, 5).Value = item.Puntuacion;

                    // Formato de puntuación con color
                    var puntuacionCell = worksheet.Cell(row, 5);
                    if (item.Puntuacion >= 80)
                        puntuacionCell.Style.Fill.BackgroundColor = XLColor.FromArgb(46, 204, 113);
                    else if (item.Puntuacion >= 60)
                        puntuacionCell.Style.Fill.BackgroundColor = XLColor.FromArgb(241, 196, 15);
                    else if (item.Puntuacion >= 40)
                        puntuacionCell.Style.Fill.BackgroundColor = XLColor.FromArgb(243, 156, 18);
                    else
                        puntuacionCell.Style.Fill.BackgroundColor = XLColor.FromArgb(231, 76, 60);

                    puntuacionCell.Style.Font.FontColor = XLColor.White;
                    puntuacionCell.Style.Font.Bold = true;
                    puntuacionCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Cell(row, 6).Value = TruncarTexto(item.Resumen, 200);
                    worksheet.Cell(row, 7).Value = TruncarTexto(item.Fortalezas, 200);
                    worksheet.Cell(row, 8).Value = TruncarTexto(item.Debilidades, 200);
                    worksheet.Cell(row, 9).Value = TruncarTexto(item.Recomendacion, 200);

                    // Estado con color
                    var estadoCell = worksheet.Cell(row, 10);
                    estadoCell.Value = item.EstadoProceso ?? "Analizado";

                    if (item.EstadoProceso == "Preseleccionado")
                        estadoCell.Style.Fill.BackgroundColor = XLColor.FromArgb(52, 152, 219);
                    else if (item.EstadoProceso == "Aprobado")
                        estadoCell.Style.Fill.BackgroundColor = XLColor.FromArgb(46, 204, 113);
                    else if (item.EstadoProceso == "Rechazado")
                        estadoCell.Style.Fill.BackgroundColor = XLColor.FromArgb(231, 76, 60);
                    else if (item.EstadoProceso == "Entrevista Programada")
                        estadoCell.Style.Fill.BackgroundColor = XLColor.FromArgb(155, 89, 182);
                    else
                        estadoCell.Style.Fill.BackgroundColor = XLColor.FromArgb(149, 165, 166);

                    estadoCell.Style.Font.FontColor = XLColor.White;
                    estadoCell.Style.Font.Bold = true;
                    estadoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Cell(row, 11).Value = item.FechaAnalisis.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cell(row, 12).Value = item.ProveedorIA ?? "No especificado";
                    worksheet.Cell(row, 13).Value = item.ModeloIA ?? "No especificado";

                    row++;
                }

                // AJUSTAR ANCHO DE COLUMNAS
                worksheet.Columns().AdjustToContents();

                // CONGELAR PANEL
                worksheet.SheetView.FreezeRows(1);

                // FILTRO AUTOMÁTICO
                worksheet.RangeUsed()?.SetAutoFilter();

                // BORDES PARA TODOS LOS DATOS
                var dataRange = worksheet.Range(1, 1, row - 1, headers.Length);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // GENERAR ARCHIVO
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var fileName = $"Resultados_{vacante.Titulo.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName
                    );
                }
            }
        }
        catch (Exception ex)
        {
            //_logger?.LogError(ex, "Error al exportar resultados");
            TempData["Error"] = "Error al generar el archivo de exportación.";
            return RedirectToAction(nameof(Resultados), new { idVacante });
        }
    }

    // =====================================================
    // EXPORTAR A CSV (ALTERNATIVA SIMPLE)
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> ExportarCsv(int idVacante)
    {
        try
        {
            var vacante = await _context.Vacantes
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.IdVacante == idVacante);

            if (vacante == null)
            {
                TempData["Error"] = "La vacante no existe.";
                return RedirectToAction(nameof(Index));
            }

            var resultados = await _context.AnalisisIA
                .AsNoTracking()
                .Include(a => a.Candidato)
                .Where(a => a.IdVacante == idVacante)
                .OrderByDescending(a => a.Puntuacion)
                .ToListAsync();

            if (!resultados.Any())
            {
                TempData["Error"] = "No hay resultados para exportar.";
                return RedirectToAction(nameof(Resultados), new { idVacante });
            }

            var sb = new System.Text.StringBuilder();

            // Encabezados
            sb.AppendLine("\"Puesto\",\"Candidato\",\"Correo\",\"Teléfono\",\"Puntuación\",\"Resumen\",\"Fortalezas\",\"Debilidades\",\"Recomendación\",\"Estado\",\"Fecha Análisis\",\"Proveedor IA\",\"Modelo IA\"");

            // Datos
            foreach (var item in resultados)
            {
                sb.AppendLine($"\"{EscapeCsv(vacante.Titulo)}\"," +
                             $"\"{EscapeCsv(item.Candidato?.NombreCompleto ?? "No disponible")}\"," +
                             $"\"{EscapeCsv(item.Candidato?.Correo ?? "No disponible")}\"," +
                             $"\"{EscapeCsv(item.Candidato?.Telefono ?? "No disponible")}\"," +
                             $"{item.Puntuacion}," +
                             $"\"{EscapeCsv(item.Resumen ?? "", 200)}\"," +
                             $"\"{EscapeCsv(item.Fortalezas ?? "", 200)}\"," +
                             $"\"{EscapeCsv(item.Debilidades ?? "", 200)}\"," +
                             $"\"{EscapeCsv(item.Recomendacion ?? "", 200)}\"," +
                             $"\"{item.EstadoProceso ?? "Analizado"}\"," +
                             $"\"{item.FechaAnalisis:dd/MM/yyyy HH:mm}\"," +
                             $"\"{EscapeCsv(item.ProveedorIA ?? "No especificado")}\"," +
                             $"\"{EscapeCsv(item.ModeloIA ?? "No especificado")}\"");
            }

            var fileName = $"Resultados_{vacante.Titulo.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(
                System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray(),
                "text/csv; charset=utf-8",
                fileName
            );
        }
        catch (Exception ex)
        {
           // _logger?.LogError(ex, "Error al exportar a CSV");
            TempData["Error"] = "Error al generar el archivo CSV.";
            return RedirectToAction(nameof(Resultados), new { idVacante });
        }
    }

    // =====================================================
    // MÉTODOS AUXILIARES
    // =====================================================

    private string TruncarTexto(string texto, int maxLength)
    {
        if (string.IsNullOrEmpty(texto))
            return string.Empty;

        return texto.Length > maxLength ? texto.Substring(0, maxLength) + "..." : texto;
    }

    private string EscapeCsv(string texto, int maxLength = 0)
    {
        if (string.IsNullOrEmpty(texto))
            return string.Empty;

        if (maxLength > 0 && texto.Length > maxLength)
            texto = texto.Substring(0, maxLength) + "...";

        return texto.Replace("\"", "\"\"");
    }
}
