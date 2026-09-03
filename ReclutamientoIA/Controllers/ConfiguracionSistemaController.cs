using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReclutamientoIA.Data;
using ReclutamientoIA.Models;

namespace ReclutamientoIA.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ConfiguracionSistemaController : Controller
    {
        private readonly ReclutamientoDbContext _context;
        private readonly IDataProtector _protector;

        public ConfiguracionSistemaController(
            ReclutamientoDbContext context,
            IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;

            _protector = dataProtectionProvider.CreateProtector(
                "ReclutamientoIA.ClaveApi"
            );
        }

        // =====================================================
        // CONFIGURACIÓN DE INTELIGENCIA ARTIFICIAL
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> InteligenciaArtificial()
        {
            var configuracion = await _context
                .ConfiguracionesIA
                .OrderBy(c => c.IdConfiguracion)
                .FirstOrDefaultAsync();

            var modelo = new ConfiguracionIAViewModel
            {
                IdConfiguracion =
                    configuracion?.IdConfiguracion ?? 0,

                Proveedor =
                    configuracion?.Proveedor ?? "OpenAI",

                Modelo =
                    configuracion?.Modelo ?? "gpt-4.1-mini",

                ClaveApi = string.Empty,

                TieneClaveGuardada =
                    !string.IsNullOrWhiteSpace(
                        configuracion?.ClaveApiProtegida
                    )
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InteligenciaArtificial(
            ConfiguracionIAViewModel modelo)
        {
            var proveedoresPermitidos = new[]
            {
                "OpenAI",
                "Gemini"
            };

            if (!proveedoresPermitidos.Contains(
                    modelo.Proveedor,
                    StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(modelo.Proveedor),
                    "El proveedor seleccionado no es válido."
                );
            }

            var configuracion = await _context
                .ConfiguracionesIA
                .OrderBy(c => c.IdConfiguracion)
                .FirstOrDefaultAsync();

            var existeClave = !string.IsNullOrWhiteSpace(
                configuracion?.ClaveApiProtegida
            );

            modelo.TieneClaveGuardada = existeClave;

            if (!existeClave &&
                string.IsNullOrWhiteSpace(modelo.ClaveApi))
            {
                ModelState.AddModelError(
                    nameof(modelo.ClaveApi),
                    "Ingrese la clave API."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            if (configuracion == null)
            {
                configuracion = new ConfiguracionIA
                {
                    Temperatura = 0.2,
                    Activo = true
                };

                _context.ConfiguracionesIA.Add(configuracion);
            }

            configuracion.Proveedor =
                modelo.Proveedor.Trim();

            configuracion.Modelo =
                modelo.Modelo.Trim();

            if (!string.IsNullOrWhiteSpace(modelo.ClaveApi))
            {
                configuracion.ClaveApiProtegida =
                    _protector.Protect(
                        modelo.ClaveApi.Trim()
                    );
            }

            configuracion.FechaActualizacion =
                DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Exito"] =
                "La configuración de IA se guardó correctamente.";

            return RedirectToAction(
                nameof(InteligenciaArtificial)
            );
        }

        // =====================================================
        // CONFIGURACIÓN DEL PROMPT
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Prompt()
        {
            var prompt = await _context
                .PromptsIA
                .OrderBy(p => p.IdPrompt)
                .FirstOrDefaultAsync();

            if (prompt == null)
            {
                prompt = new PromptIA
                {
                    Nombre = "Análisis de currículum",
                    Tipo = "AnalisisCV",
                    Contenido = string.Empty,
                    Activo = true,
                    FechaCreacion = DateTime.Now,
                    FechaActualizacion = DateTime.Now
                };
            }

            return View(prompt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Prompt(
            int idPrompt,
            string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
            {
                ModelState.AddModelError(
                    nameof(contenido),
                    "El prompt no puede quedar vacío."
                );
            }

            if (!ModelState.IsValid)
            {
                var modelo = new PromptIA
                {
                    IdPrompt = idPrompt,
                    Nombre = "Análisis de currículum",
                    Tipo = "AnalisisCV",
                    Contenido = contenido ?? string.Empty,
                    Activo = true
                };

                return View(modelo);
            }

            var prompt = await _context
                .PromptsIA
                .FirstOrDefaultAsync(
                    p => p.IdPrompt == idPrompt
                );

            if (prompt == null)
            {
                prompt = await _context
                    .PromptsIA
                    .OrderBy(p => p.IdPrompt)
                    .FirstOrDefaultAsync();
            }

            if (prompt == null)
            {
                prompt = new PromptIA
                {
                    Nombre = "Análisis de currículum",
                    Tipo = "AnalisisCV",
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                _context.PromptsIA.Add(prompt);
            }

            prompt.Contenido = contenido.Trim();
            prompt.FechaActualizacion = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Exito"] =
                "El prompt se actualizó correctamente.";

            return RedirectToAction(nameof(Prompt));
        }
    }
}
