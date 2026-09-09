using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReclutamientoIA.Models;

namespace ReclutamientoIA.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsuariosController(
            UserManager<Usuario> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = await _userManager.Users.ToListAsync();
            var lista = new List<UsuarioListaViewModel>();

            foreach (var usuario in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(usuario);

                lista.Add(new UsuarioListaViewModel
                {
                    Id = usuario.Id,
                    NombreCompleto = usuario.NombreCompleto,
                    Correo = usuario.Email ?? string.Empty,
                    Telefono = usuario.PhoneNumber,
                    Rol = string.Join(", ", roles),
                    Activo = usuario.Activo
                });
            }

            return View(lista);
        }

        [HttpGet]
        public IActionResult Create()
        {
            CargarRoles();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CrearUsuarioViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                CargarRoles();
                return View(modelo);
            }

            var existente =
                await _userManager.FindByEmailAsync(modelo.Correo);

            if (existente != null)
            {
                ModelState.AddModelError(
                    nameof(modelo.Correo),
                    "Ya existe un usuario con este correo."
                );

                CargarRoles();
                return View(modelo);
            }

            if (!await _roleManager.RoleExistsAsync(modelo.Rol))
            {
                ModelState.AddModelError(
                    nameof(modelo.Rol),
                    "El rol seleccionado no existe."
                );

                CargarRoles();
                return View(modelo);
            }

            var usuario = new Usuario
            {
                NombreCompleto = modelo.NombreCompleto,
                UserName = modelo.Correo,
                Email = modelo.Correo,
                PhoneNumber = modelo.Telefono,
                EmailConfirmed = true,
                Activo = true
            };

            var resultado = await _userManager.CreateAsync(
                usuario,
                modelo.Contrasena
            );

            if (resultado.Succeeded)
            {
                await _userManager.AddToRoleAsync(
                    usuario,
                    modelo.Rol
                );

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            CargarRoles();
            return View(modelo);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var usuario = await _userManager.FindByIdAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(usuario);

            var modelo = new EditarUsuarioViewModel
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto,
                Correo = usuario.Email ?? string.Empty,
                Telefono = usuario.PhoneNumber,
                Rol = roles.FirstOrDefault() ?? string.Empty,
                Activo = usuario.Activo
            };

            CargarRoles();
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            EditarUsuarioViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                CargarRoles();
                return View(modelo);
            }

            var usuario = await _userManager.FindByIdAsync(modelo.Id);

            if (usuario == null)
            {
                return NotFound();
            }

            var usuarioActualId = _userManager.GetUserId(User);

            if (usuario.Id == usuarioActualId &&
                (!modelo.Activo || modelo.Rol != "Administrador"))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "No puede desactivar su propia cuenta ni quitarse el rol Administrador."
                );

                CargarRoles();
                return View(modelo);
            }

            var usuarioConCorreo =
                await _userManager.FindByEmailAsync(modelo.Correo);

            if (usuarioConCorreo != null &&
                usuarioConCorreo.Id != usuario.Id)
            {
                ModelState.AddModelError(
                    nameof(modelo.Correo),
                    "El correo ya pertenece a otro usuario."
                );

                CargarRoles();
                return View(modelo);
            }

            if (!await _roleManager.RoleExistsAsync(modelo.Rol))
            {
                ModelState.AddModelError(
                    nameof(modelo.Rol),
                    "El rol seleccionado no existe."
                );

                CargarRoles();
                return View(modelo);
            }

            usuario.NombreCompleto = modelo.NombreCompleto;
            usuario.Email = modelo.Correo;
            usuario.UserName = modelo.Correo;
            usuario.PhoneNumber = modelo.Telefono;
            usuario.Activo = modelo.Activo;

            var resultado = await _userManager.UpdateAsync(usuario);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description
                    );
                }

                CargarRoles();
                return View(modelo);
            }

            var rolesActuales =
                await _userManager.GetRolesAsync(usuario);

            if (!rolesActuales.Contains(modelo.Rol))
            {
                await _userManager.RemoveFromRolesAsync(
                    usuario,
                    rolesActuales
                );

                await _userManager.AddToRoleAsync(
                    usuario,
                    modelo.Rol
                );
            }
            await _userManager.UpdateSecurityStampAsync(usuario);
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> RestablecerContrasena(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var usuario = await _userManager.FindByIdAsync(id);

            if (usuario == null)
            {
                return NotFound();
            }

            var modelo = new RestablecerContrasenaViewModel
            {
                Id = usuario.Id,
                NombreCompleto = usuario.NombreCompleto
            };

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestablecerContrasena(
            RestablecerContrasenaViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var usuario = await _userManager.FindByIdAsync(modelo.Id);

            if (usuario == null)
            {
                return NotFound();
            }

            var token =
                await _userManager.GeneratePasswordResetTokenAsync(usuario);

            var resultado = await _userManager.ResetPasswordAsync(
                usuario,
                token,
                modelo.NuevaContrasena
            );

            if (resultado.Succeeded)
            {
                TempData["Mensaje"] =
                    $"La contraseña de {usuario.NombreCompleto} fue actualizada.";

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description
                );                 
            }

            modelo.NombreCompleto = usuario.NombreCompleto;
            return View(modelo);
        }

        private void CargarRoles()
        {
            ViewBag.Roles = _roleManager.Roles
                .OrderBy(rol => rol.Name)
                .ToList();
        }
    }
}