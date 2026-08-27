using Microsoft.AspNetCore.Identity;
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