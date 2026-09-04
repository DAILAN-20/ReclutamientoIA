using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReclutamientoIA.Models;

namespace ReclutamientoIA.Data
{
    public static class InicializadorDatos
    {
        public static async Task InicializarAsync(
            IServiceProvider servicios,
            IConfiguration configuracion)
        {
            var roleManager =
                servicios.GetRequiredService<
                    RoleManager<IdentityRole>
                >();

            var userManager =
                servicios.GetRequiredService<
                    UserManager<Usuario>
                >();

            var contexto =
                servicios.GetRequiredService<
                    ReclutamientoDbContext
                >();

            // =================================================
            // CREAR ROLES
            // =================================================

            string[] roles =
            {
                "Administrador",
                "Reclutador"
            };

            foreach (var nombreRol in roles)
            {
                var existeRol =
                    await roleManager.RoleExistsAsync(
                        nombreRol
                    );

                if (!existeRol)
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(nombreRol)
                    );
                }
            }

            // =================================================
            // CREAR CONFIGURACIÓN INICIAL DE IA
            // =================================================

            var existeConfiguracionIA =
                await contexto.ConfiguracionesIA.AnyAsync();

            if (!existeConfiguracionIA)
            {
                contexto.ConfiguracionesIA.Add(
                    new ConfiguracionIA
                    {
                        Proveedor = "OpenAI",
                        Modelo = "gpt-5-mini",

                        // Se configurará desde la pantalla
                        // Configuración de IA.
                        ClaveApiProtegida = null,

                        // Estos campos se mantienen para
                        // conservar compatibilidad con el sistema.
                        Temperatura = 0.2,
                        Activo = true,

                        FechaActualizacion = DateTime.Now
                    }
                );
            }

            // =================================================
            // CREAR PROMPT INICIAL
            // =================================================

            var existePromptAnalisis =
                await contexto.PromptsIA.AnyAsync(
                    prompt =>
                        prompt.Tipo == "AnalisisCV"
                );

            if (!existePromptAnalisis)
            {
                contexto.PromptsIA.Add(
                    new PromptIA
                    {
                        Nombre =
                            "Análisis de currículum",

                        Tipo =
                            "AnalisisCV",

                        Activo = true,

                        Contenido = """
                        Actúa como especialista en reclutamiento.

                        Analiza el currículum comparándolo únicamente
                        con la vacante y sus requisitos.

                        No inventes información que no aparezca
                        expresamente en el currículum.

                        Devuelve obligatoriamente:

                        - nombre
                        - correo
                        - telefono
                        - puntuacion de 0 a 100
                        - resumen
                        - fortalezas
                        - debilidades
                        - recomendacion

                        La puntuación debe representar el nivel de
                        compatibilidad real entre el candidato y
                        la vacante.

                        Si algún dato personal no aparece en el
                        currículum, devuelve una cadena vacía.

                        La recomendación debe indicar claramente
                        si el candidato debe continuar o no en
                        el proceso de selección.
                        """,

                        FechaCreacion =
                            DateTime.Now,

                        FechaActualizacion =
                            DateTime.Now
                    }
                );
            }

            // Guardar la configuración y el prompt antes
            // de continuar con el administrador inicial.
            await contexto.SaveChangesAsync();

            // =================================================
            // CREAR ADMINISTRADOR INICIAL
            // =================================================

            var nombre =
                configuracion[
                    "AdministradorInicial:Nombre"
                ];

            var correo =
                configuracion[
                    "AdministradorInicial:Correo"
                ];

            var contrasena =
                configuracion[
                    "AdministradorInicial:Contrasena"
                ];

            if (string.IsNullOrWhiteSpace(correo) ||
                string.IsNullOrWhiteSpace(contrasena))
            {
                return;
            }

            var administrador =
                await userManager.FindByEmailAsync(
                    correo
                );

            if (administrador == null)
            {
                administrador = new Usuario
                {
                    NombreCompleto =
                        nombre ?? "Administrador",

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
            else
            {
                // Asegurar que el usuario existente
                // tenga el rol de administrador.
                var esAdministrador =
                    await userManager.IsInRoleAsync(
                        administrador,
                        "Administrador"
                    );

                if (!esAdministrador)
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