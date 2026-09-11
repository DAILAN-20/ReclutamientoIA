using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReclutamientoIA.Data;
using ReclutamientoIA.Models;
using System.Diagnostics;

namespace ReclutamientoIA.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ReclutamientoDbContext _context;

    public HomeController(
        ReclutamientoDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var nombreUsuario = User.Identity?.Name ?? "Usuario";

        if (nombreUsuario.Contains("@"))
        {
            nombreUsuario = nombreUsuario.Split('@')[0];
        }

        var modelo = new DashboardViewModel
        {
            NombreUsuario = nombreUsuario,

            VacantesActivas = await _context.Vacantes
                .AsNoTracking()
                .CountAsync(v =>
                    v.Estado == "Activa" ||
                    v.Estado == "Activo"
                ),

            CurriculumsRegistrados = await _context.Candidatos
                .AsNoTracking()
                .CountAsync(),

            AnalizadosIA = await _context.AnalisisIA
                .AsNoTracking()
                .CountAsync(),

            Preseleccionados = await _context.AnalisisIA
                .AsNoTracking()
                .CountAsync(a =>
                    a.EstadoProceso == "Preseleccionado"
                ),

            EntrevistasPendientes = await _context.Entrevistas
                .AsNoTracking()
                .CountAsync(e =>
                    e.EstadoEntrevista == "Pendiente"
                ),

            CandidatosAprobados = await _context.AnalisisIA
                .AsNoTracking()
                .CountAsync(a =>
                    a.EstadoProceso == "Aprobado"
                )
        };

        modelo.Vacantes = await _context.Vacantes
            .AsNoTracking()
            .Where(v =>
                v.Estado == "Activa" ||
                v.Estado == "Activo"
            )
            .OrderByDescending(v => v.FechaCreacion)
            .Take(5)
            .Select(v => new VacanteDashboardViewModel
            {
                IdVacante = v.IdVacante,

                Titulo = v.Titulo,

                Estado = v.Estado,

                CantidadCandidatos = _context.AnalisisIA
                    .Count(a =>
                        a.IdVacante == v.IdVacante
                    ),

                Preseleccionados = _context.AnalisisIA
                    .Count(a =>
                        a.IdVacante == v.IdVacante &&
                        a.EstadoProceso == "Preseleccionado"
                    ),

                MayorPuntuacion = _context.AnalisisIA
                    .Where(a =>
                        a.IdVacante == v.IdVacante
                    )
                    .Select(a => (int?)a.Puntuacion)
                    .Max() ?? 0
            })
            .ToListAsync();

        modelo.CandidatosDestacados =
            await _context.AnalisisIA
                .AsNoTracking()
                .Include(a => a.Candidato)
                .Include(a => a.Vacante)
                .OrderByDescending(a => a.Puntuacion)
                .Take(5)
                .Select(a =>
                    new CandidatoDestacadoViewModel
                    {
                        IdAnalisis = a.IdAnalisis,

                        IdVacante = a.IdVacante,

                        NombreCompleto =
                            a.Candidato.NombreCompleto ??
                            "Nombre no disponible",

                        Vacante = a.Vacante.Titulo,

                        Puntuacion = a.Puntuacion,

                        Estado =
                            a.EstadoProceso ??
                            "Analizado"
                    }
                )
                .ToListAsync();

        return View(modelo);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true
    )]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel
            {
                RequestId =
                    Activity.Current?.Id ??
                    HttpContext.TraceIdentifier
            }
        );
    }
}