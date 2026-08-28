using Microsoft.AspNetCore.Identity;
using ReclutamientoIA.Models;
using Microsoft.EntityFrameworkCore;

namespace ReclutamientoIA.Data
{
    public static class InicializadorDatos
    {
        public static async Task InicializarAsync(
            IServiceProvider servicios,
            IConfiguration configuracion)
        {
            var roleManager =
                servicios.GetRequiredService<RoleManager<IdentityRole>>();

            var userManager =
                servicios.GetRequiredService<UserManager<Usuario>>();

            string[] roles = { "Administrador", "Reclutador" };

            foreach (var nombreRol in roles)
            {
                if (!await roleManager.RoleExistsAsync(nombreRol))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(nombreRol)
                    );
                }
            }
            var contexto =
    servicios.GetRequiredService<ReclutamientoDbContext>();

            if (!await contexto.ConfiguracionesIA.AnyAsync())
            {
                contexto.ConfiguracionesIA.Add(
                    new ConfiguracionIA
                    {
                        Proveedor = "OpenAI",
                        Modelo = "gpt-5.6-luna",
                        Temperatura = 0.2,
                        Activo = true
                    }
                );
            }

            if (!await contexto.PromptsIA.AnyAsync(
                    p => p.Tipo == "AnalisisCV"))
            {
                contexto.PromptsIA.Add(
                    new PromptIA
                    {
                        Nombre = "Análisis de currículum",
                        Tipo = "AnalisisCV",
                        Activo = true,
                        Contenido = """
                Actúa como especialista en reclutamiento.

                Analiza el currículum comparándolo únicamente
                con la vacante y sus requisitos.

                No inventes información que no aparezca en el CV.

                Devuelve:
                - nombre
                - correo
                - telefono
                - puntuacion de 0 a 100
                - resumen
                - fortalezas
                - debilidades
                - recomendacion

                La puntuación debe representar el nivel de
                compatibilidad real con la vacante.
                """
                    }
                );
            }

            await contexto.SaveChangesAsync();

            var nombre = configuracion["AdministradorInicial:Nombre"];
            var correo = configuracion["AdministradorInicial:Correo"];
            var contrasena =
                configuracion["AdministradorInicial:Contrasena"];

            if (string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrWhiteSpace(contrasena))
            {
                return;
            }

            var administrador =
                await userManager.FindByEmailAsync(correo);

            if (administrador == null)
            {
                administrador = new Usuario
                {
                    NombreCompleto = nombre ?? "Administrador",
                    UserName = correo,
                    Email = correo,
                    EmailConfirmed = true,
                    Activo = true
                };

                var resultado =
                    await userManager.CreateAsync(
                        administrador,
                        contrasena
                    );

                if (resultado.Succeeded)
                {
                    await userManager.AddToRoleAsync(
                        administrador,
                        "Administrador"
                    );
                }
            }
        }
    }
}