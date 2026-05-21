using App_Clinica.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace App_Clinica.Controllers
{
    public class LoginController : Controller
    {
        private readonly NotificacionClinicaContext _context;

        public LoginController(NotificacionClinicaContext context)
        {
            _context = context;
        }
        public IActionResult login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> login(Login login)
        {
            if (ModelState.IsValid)
            {
                // Aquí ya deberías tener los valores del formulario
                var numeroIdentificacion = login.NumeroIdentificacion;
                var contraseña = login.Contraseña;

                // Consulta a la base de datos para validar las credenciales
                var usuarioValido = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.NumeroIdentificacion == numeroIdentificacion && u.Contraseña == contraseña);

                if (usuarioValido != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, usuarioValido.NumeroIdentificacion),
                        new Claim(ClaimTypes.Role, usuarioValido.IdTipoUsuario.ToString()) // Si tienes roles, puedes agregarlos aquí
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    // Autenticar y redirigir al usuario
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    // Si las credenciales son correctas, redirige al inicio
                    return RedirectToAction("Index", "Usuario");
                }
                else
                {
                    // Credenciales incorrectas, regresa a la vista de login con un mensaje de error
                    ModelState.AddModelError(string.Empty, "Identificación o contraseña incorrecta.");
                }
            }

            // Si el modelo no es válido, regresas a la vista con los errores de validación
            return View(login);
        }

        public async Task<IActionResult> Logout()
        {
            // Cerrar sesión eliminando la cookie de autenticación
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirigir a la página de inicio (o a donde desees)
            return RedirectToAction("Inicio", "Usuario");
        }

    }
}
