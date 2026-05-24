using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App_Clinica.Models;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using RabbitMQ.Client;
using System.Text.Json;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using static System.Runtime.InteropServices.JavaScript.JSType;
using BCrypt.Net;

namespace App_Clinica.Controllers
{
    public class UsuarioController : Controller
    {
        private readonly NotificacionClinicaContext _context;

        public UsuarioController(NotificacionClinicaContext context)
        {
            _context = context;
        }

        // GET: Usuario
        public async Task<IActionResult> Index()
        {
            //var notificacionClinicaContext = _context.Usuarios.Include(u => u.IdTipoUsuarioNavigation);
            //return View(await notificacionClinicaContext.ToListAsync());

            // Obtén el número de identificación del usuario logueado desde los Claims
            var numeroIdentificacion = User.Identity.Name;

            // Buscar el usuario logueado en la base de datos
            var usuarioLogueado = await _context.Usuarios
                .Include(u => u.IdTipoUsuarioNavigation)
                .FirstOrDefaultAsync(u => u.NumeroIdentificacion == numeroIdentificacion);

            // Devolver la vista con el usuario logueado
            return View(usuarioLogueado);
        }

        // GET: Usuario/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .Include(u => u.IdTipoUsuarioNavigation)
                .FirstOrDefaultAsync(m => m.IdUsuario == id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // GET: Usuario/Create
        public IActionResult Create()
        {
            //ViewData["IdTipoUsuario"] = new SelectList(_context.TipoUsuarios, "IdTipoUsuario", "IdTipoUsuario");
            //return View();
            var tipoUsuarios = _context.TipoUsuarios.Select(t => new SelectListItem
            {
                Value = t.IdTipoUsuario
                .ToString(),
                Text = t.NombreTipo // Aquí asignas el nombre en lugar del Id
            }).ToList();

            ViewBag.IdTipoUsuario = tipoUsuarios;
            return View();
        }

        public IActionResult Inicio()
        {
            return View();
        }

        // POST: Usuario/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdUsuario,IdTipoUsuario,NumeroIdentificacion,Contraseña,Nombre,Apellido,Especialidad,Disponibilidad,Email,Telefono,FechaRegistro")] Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                var existe = await _context.Usuarios
                    .AnyAsync(u => u.NumeroIdentificacion == usuario.NumeroIdentificacion);

                if (existe)
                {
                    ModelState.AddModelError(nameof(usuario.NumeroIdentificacion),
                        "El número de identificación ingresado ya se encuentra registrado.");
                }
                else
                {
                    usuario.Contraseña = BCrypt.Net.BCrypt.HashPassword(usuario.Contraseña);
                    _context.Add(usuario);
                    await _context.SaveChangesAsync();

                    TempData["RegistroExitoso"] = $"¡Registro exitoso! Bienvenido, {usuario.Nombre}.";

                    var message = $"Hola {usuario.Nombre}, tu registro al servicio en linea de citas medicas de Sura EPS, es exitoso";
                    var messageSerialize = JsonSerializer.Serialize(message);
                    PublishEvent("RegistroCreado", messageSerialize, usuario.Email);

                    return RedirectToAction("login", "Login");
                }
            }

            var tipoUsuarios = _context.TipoUsuarios.Select(t => new SelectListItem
            {
                Value = t.IdTipoUsuario.ToString(),
                Text = t.NombreTipo
            }).ToList();

            ViewBag.IdTipoUsuario = tipoUsuarios;
            return View(usuario);
        }

        // GET: Usuario/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }
            ViewData["IdTipoUsuario"] = new SelectList(_context.TipoUsuarios, "IdTipoUsuario", "IdTipoUsuario", usuario.IdTipoUsuario);
            return View(usuario);
        }

        // POST: Usuario/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdUsuario,IdTipoUsuario,NumeroIdentificacion,Contraseña,Nombre,Apellido,Especialidad,Disponibilidad,Email,Telefono,FechaRegistro")] Usuario usuario)
        {
            if (id != usuario.IdUsuario)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(usuario.Contraseña))
                    {
                        var existing = await _context.Usuarios.AsNoTracking()
                            .FirstOrDefaultAsync(u => u.IdUsuario == id);
                        usuario.Contraseña = existing?.Contraseña ?? string.Empty;
                    }
                    else
                    {
                        usuario.Contraseña = BCrypt.Net.BCrypt.HashPassword(usuario.Contraseña);
                    }

                    _context.Update(usuario);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(usuario.IdUsuario))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdTipoUsuario"] = new SelectList(_context.TipoUsuarios, "IdTipoUsuario", "IdTipoUsuario", usuario.IdTipoUsuario);
            return View(usuario);
        }

        // GET: Usuario/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuario = await _context.Usuarios
                .Include(u => u.IdTipoUsuarioNavigation)
                .FirstOrDefaultAsync(m => m.IdUsuario == id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // POST: Usuario/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("login", "Login");
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.IdUsuario == id);
        }

        public void PublishEvent(string eventName, string messageSerialize, string email)
        {
            try
            {

                var factory = new ConnectionFactory() { HostName = "localhost" };
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;
                    channel.ConfirmSelect();
                    channel.ExchangeDeclare(exchange: "clinic_events", type: "topic", durable: true);
                    var payload = new
                    {
                        CorreoDestinatario = email,
                        MensajeActualizado = messageSerialize
                    };

                    var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
                    channel.BasicPublish(exchange: "clinic_events",
                                         routingKey: eventName,
                                         basicProperties: properties,
                                         body: body);

                    if (!channel.WaitForConfirms())
                    {
                        Console.WriteLine("Failed to publish message");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing message: {ex.Message}");
            }

        }

    }
}
