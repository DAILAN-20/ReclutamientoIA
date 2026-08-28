using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReclutamientoIA.Data;
using ReclutamientoIA.Models;
using ReclutamientoIA.Services;

namespace ReclutamientoIA.Controllers
{
    [Authorize(Roles = "Administrador,Reclutador")]
    public class CandidatosController : Controller
    {
        private readonly ReclutamientoDbContext _context;
        private readonly ICurriculumService _curriculumService;

        public CandidatosController(
            ReclutamientoDbContext context,
            ICurriculumService curriculumService)
        {
            _context = context;
            _curriculumService = curriculumService;
        }

        public async Task<IActionResult> Index()
        {
            var candidatos = await _context.Candidatos
                .AsNoTracking()
                .OrderByDescending(c => c.FechaRegistro)
                .ToListAsync();

            return View(candidatos);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            SubirCandidatoViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            if (!_curriculumService.EsPdfValido(modelo.ArchivoCv))
            {
                ModelState.AddModelError(
                    nameof(modelo.ArchivoCv),
                    "El archivo debe ser un PDF válido y no superar 10 MB."
                );

                return View(modelo);
            }

            var hash = await _curriculumService.CalcularHashAsync(
                modelo.ArchivoCv
            );

            var duplicado = await _context.Candidatos
                .AsNoTracking()
                .AnyAsync(c => c.HashArchivo == hash);

            if (duplicado)
            {
                ModelState.AddModelError(
                    nameof(modelo.ArchivoCv),
                    "Este currículum ya fue registrado."
                );

                return View(modelo);
            }

            var nombreGuardado =
                await _curriculumService.GuardarAsync(
                    modelo.ArchivoCv
                );

            var candidato = new Candidato
            {
                NombreCompleto = modelo.NombreCompleto,
                Correo = modelo.Correo,
                Telefono = modelo.Telefono,
                NombreArchivoOriginal =
                    Path.GetFileName(modelo.ArchivoCv.FileName),
                NombreArchivoGuardado = nombreGuardado,
                TipoContenido = "application/pdf",
                TamanoArchivo = modelo.ArchivoCv.Length,
                HashArchivo = hash,
                FechaRegistro = DateTime.Now,
                Activo = true
            };

            try
            {
                _context.Candidatos.Add(candidato);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] =
                    "El candidato y su currículum fueron registrados.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                _curriculumService.Eliminar(nombreGuardado);

                ModelState.AddModelError(
                    string.Empty,
                    "No fue posible registrar el candidato. Verifique que el currículum no esté duplicado."
                );

                return View(modelo);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Descargar(int id)
        {
            var candidato = await _context.Candidatos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCandidato == id);

            if (candidato == null)
            {
                return NotFound();
            }

            var archivo = _curriculumService.AbrirLectura(
                candidato.NombreArchivoGuardado
            );

            if (archivo == null)
            {
                return NotFound();
            }

            return File(
                archivo,
                candidato.TipoContenido,
                candidato.NombreArchivoOriginal
            );
        }
    }
}