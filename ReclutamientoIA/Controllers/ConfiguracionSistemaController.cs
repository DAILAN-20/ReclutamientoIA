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

        private readonly IDataProtector
            _protectorIA;

        private readonly IDataProtector
            _protectorCorreo;

        public ConfiguracionSistemaController(
            ReclutamientoDbContext context,
            IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;

            _protectorIA =
                dataProtectionProvider.CreateProtector(
                    "ReclutamientoIA.ClaveApi"
                );

            _protectorCorreo =
                dataProtectionProvider.CreateProtector(
                    "ReclutamientoIA.ClaveCorreo"
                );
        }

        // =====================================================
        // CONFIGURACIÓN DE IA - MOSTRAR
        // =====================================================

        [HttpGet]
        public async Task<IActionResult>
            InteligenciaArtificial()
        {
            var configuracion = await _context
                .ConfiguracionesIA
                .OrderBy(c => c.IdConfiguracion)
                .FirstOrDefaultAsync();

            var modelo =
                new ConfiguracionIAViewModel
                {
                    IdConfiguracion =
                        configuracion?
                            .IdConfiguracion ?? 0,

                    Proveedor =
                        configuracion?
                            .Proveedor ??
                        "OpenAI",

                    Modelo =
                        configuracion?
                            .Modelo ??
                        "gpt-5-mini",

                    ClaveApi =
                        string.Empty,

                    TieneClaveGuardada =
                        !string.IsNullOrWhiteSpace(
                            configuracion?
                                .ClaveApiProtegida
                        )
                };

            return View(modelo);
        }

        // =====================================================
        // CONFIGURACIÓN DE IA - GUARDAR
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            InteligenciaArtificial(
                ConfiguracionIAViewModel modelo)
        {
            var formulario =
                await Request.ReadFormAsync();

            var claveRecibida =
                formulario["ClaveApi"]
                    .ToString()
                    .Trim();

            modelo.ClaveApi =
                claveRecibida;

            var proveedoresPermitidos =
                new[]
                {
                    "OpenAI",
                    "Gemini"
                };

            if (string.IsNullOrWhiteSpace(
                    modelo.Proveedor) ||
                !proveedoresPermitidos.Contains(
                    modelo.Proveedor,
                    StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(modelo.Proveedor),
                    "El proveedor seleccionado no es válido."
                );
            }

            if (string.IsNullOrWhiteSpace(
                    modelo.Modelo))
            {
                ModelState.AddModelError(
                    nameof(modelo.Modelo),
                    "Seleccione un modelo de IA."
                );
            }

            var configuracion = await _context
                .ConfiguracionesIA
                .OrderBy(c => c.IdConfiguracion)
                .FirstOrDefaultAsync();

            var existeClave =
                !string.IsNullOrWhiteSpace(
                    configuracion?
                        .ClaveApiProtegida
                );

            modelo.TieneClaveGuardada =
                existeClave;

            if (!existeClave &&
                string.IsNullOrWhiteSpace(
                    claveRecibida))
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
                configuracion =
                    new ConfiguracionIA
                    {
                        Temperatura = 0.2,
                        Activo = true,
                        FechaActualizacion =
                            DateTime.Now
                    };

                _context.ConfiguracionesIA.Add(
                    configuracion
                );
            }

            configuracion.Proveedor =
                modelo.Proveedor.Trim();

            configuracion.Modelo =
                modelo.Modelo.Trim();

            if (!string.IsNullOrWhiteSpace(
                    claveRecibida))
            {
                configuracion.ClaveApiProtegida =
                    _protectorIA.Protect(
                        claveRecibida
                    );
            }

            configuracion.Activo =
                true;

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
        // CONFIGURACIÓN DEL CORREO - MOSTRAR
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Correo()
        {
            var configuracion = await _context
                .ConfiguracionesCorreo
                .OrderBy(c =>
                    c.IdConfiguracionCorreo
                )
                .FirstOrDefaultAsync();

            var modelo =
                new ConfiguracionCorreoViewModel
                {
                    IdConfiguracionCorreo =
                        configuracion?
                            .IdConfiguracionCorreo ?? 0,

                    ServidorSmtp =
                        configuracion?
                            .ServidorSmtp ??
                        "smtp.gmail.com",

                    PuertoSmtp =
                        configuracion?
                            .PuertoSmtp ?? 587,

                    ServidorImap =
                        configuracion?
                            .ServidorImap ??
                        "imap.gmail.com",

                    PuertoImap =
                        configuracion?
                            .PuertoImap ?? 993,

                    Usuario =
                        configuracion?
                            .Usuario ??
                        string.Empty,

                    Clave =
                        string.Empty,

                    CorreoRemitente =
                        configuracion?
                            .CorreoRemitente ??
                        string.Empty,

                    NombreRemitente =
                        configuracion?
                            .NombreRemitente ??
                        "Reclutamiento IA",

                    TieneClaveGuardada =
                        !string.IsNullOrWhiteSpace(
                            configuracion?
                                .ClaveProtegida
                        )
                };

            return View(modelo);
        }

        // =====================================================
        // CONFIGURACIÓN DEL CORREO - GUARDAR
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Correo(
            ConfiguracionCorreoViewModel modelo)
        {
            var formulario =
                await Request.ReadFormAsync();

            var claveRecibida =
                formulario["Clave"]
                    .ToString()
                    .Trim();

            modelo.Clave =
                claveRecibida;

            var configuracion = await _context
                .ConfiguracionesCorreo
                .OrderBy(c =>
                    c.IdConfiguracionCorreo
                )
                .FirstOrDefaultAsync();

            var existeClave =
                !string.IsNullOrWhiteSpace(
                    configuracion?
                        .ClaveProtegida
                );

            modelo.TieneClaveGuardada =
                existeClave;

            if (!existeClave &&
                string.IsNullOrWhiteSpace(
                    claveRecibida))
            {
                ModelState.AddModelError(
                    nameof(modelo.Clave),
                    "Ingrese la contraseña de aplicación."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            if (configuracion == null)
            {
                configuracion =
                    new ConfiguracionCorreo
                    {
                        Activo = true,
                        FechaActualizacion =
                            DateTime.Now
                    };

                _context.ConfiguracionesCorreo.Add(
                    configuracion
                );
            }

            configuracion.ServidorSmtp =
                modelo.ServidorSmtp.Trim();

            configuracion.PuertoSmtp =
                modelo.PuertoSmtp;

            configuracion.ServidorImap =
                modelo.ServidorImap.Trim();

            configuracion.PuertoImap =
                modelo.PuertoImap;

            configuracion.Usuario =
                modelo.Usuario.Trim();

            configuracion.CorreoRemitente =
                modelo.CorreoRemitente.Trim();

            configuracion.NombreRemitente =
                modelo.NombreRemitente.Trim();

            if (!string.IsNullOrWhiteSpace(
                    claveRecibida))
            {
                configuracion.ClaveProtegida =
                    _protectorCorreo.Protect(
                        claveRecibida
                    );
            }

            configuracion.Activo =
                true;

            configuracion.FechaActualizacion =
                DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Exito"] =
                "La configuración del correo se guardó correctamente.";

            return RedirectToAction(
                nameof(Correo)
            );
        }

        // =====================================================
        // PROMPT - MOSTRAR
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Prompt()
        {
            var prompt = await _context
                .PromptsIA
                .Where(p =>
                    p.Tipo == "AnalisisCV"
                )
                .OrderBy(p => p.IdPrompt)
                .FirstOrDefaultAsync();

            if (prompt == null)
            {
                prompt =
                    new PromptIA
                    {
                        Nombre =
                            "Análisis de currículum",

                        Tipo =
                            "AnalisisCV",

                        Contenido =
                            string.Empty,

                        Activo =
                            true,

                        FechaCreacion =
                            DateTime.Now,

                        FechaActualizacion =
                            DateTime.Now
                    };
            }

            return View(prompt);
        }

        // =====================================================
        // PROMPT - GUARDAR
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Prompt(
            int idPrompt,
            string contenido)
        {
            if (string.IsNullOrWhiteSpace(
                    contenido))
            {
                ModelState.AddModelError(
                    nameof(contenido),
                    "El prompt no puede quedar vacío."
                );
            }

            if (!ModelState.IsValid)
            {
                var modelo =
                    new PromptIA
                    {
                        IdPrompt =
                            idPrompt,

                        Nombre =
                            "Análisis de currículum",

                        Tipo =
                            "AnalisisCV",

                        Contenido =
                            contenido ??
                            string.Empty,

                        Activo =
                            true,

                        FechaActualizacion =
                            DateTime.Now
                    };

                return View(modelo);
            }

            var prompt = await _context
                .PromptsIA
                .FirstOrDefaultAsync(
                    p =>
                        p.IdPrompt == idPrompt
                );

            if (prompt == null)
            {
                prompt = await _context
                    .PromptsIA
                    .Where(p =>
                        p.Tipo == "AnalisisCV"
                    )
                    .OrderBy(p => p.IdPrompt)
                    .FirstOrDefaultAsync();
            }

            if (prompt == null)
            {
                prompt =
                    new PromptIA
                    {
                        Nombre =
                            "Análisis de currículum",

                        Tipo =
                            "AnalisisCV",

                        Activo =
                            true,

                        FechaCreacion =
                            DateTime.Now
                    };

                _context.PromptsIA.Add(
                    prompt
                );
            }

            prompt.Contenido =
                contenido.Trim();

            prompt.Activo =
                true;

            prompt.FechaActualizacion =
                DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Exito"] =
                "El prompt se actualizó correctamente.";

            return RedirectToAction(
                nameof(Prompt)
            );
        }
    }
}