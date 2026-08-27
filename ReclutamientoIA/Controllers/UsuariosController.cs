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

        private void CargarRoles()
        {
            ViewBag.Roles = _roleManager.Roles
                .OrderBy(rol => rol.Name)
                .ToList();
        }
    }
}