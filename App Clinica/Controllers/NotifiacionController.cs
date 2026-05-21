using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using App_Clinica.Models;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Text.Json;
using RabbitMQ.Client;
using System.Text;

namespace App_Clinica.Controllers
{
    public class NotifiacionController : Controller
    {
        private readonly NotificacionClinicaContext _context;

        public NotifiacionController(NotificacionClinicaContext context)
        {
            _context = context;
        }

        // GET: Notifiacion
        public async Task<IActionResult> Index()
        {
            var notificacionClinicaContext = _context.Notifiacions.Include(n => n.IdCitaNavigation);
            return View(await notificacionClinicaContext.ToListAsync());
        }

        // GET: Notifiacion/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notifiacion = await _context.Notifiacions
                .Include(n => n.IdCitaNavigation)
                .FirstOrDefaultAsync(m => m.IdRecordatorio == id);
            if (notifiacion == null)
            {
                return NotFound();
            }

            return View(notifiacion);
        }

        // GET: Notifiacion/Create
        public IActionResult Create()
        {
            ViewData["IdCita"] = new SelectList(_context.CitaMedicas, "IdCita", "IdCita");
            return View();
        }

        // POST: Notifiacion/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdRecordatorio,IdCita,FechaEnvio,Mensaje,Enviada")] Notifiacion notifiacion)
        {
            if (ModelState.IsValid)
            {
                var existeNotificacion = await _context.Notifiacions
                    .AnyAsync(n => n.IdCita == notifiacion.IdCita);

                if (existeNotificacion)
                {
                    return Ok();
                }

                _context.Add(notifiacion);
                await _context.SaveChangesAsync();

                // Cargar las relaciones necesarias para obtener el email
                await _context.Entry(notifiacion)
                    .Reference(n => n.IdCitaNavigation)
                    .Query()
                    .Include(c => c.IdAfiliadoNavigation)
                    .LoadAsync();

                // Verificar que las propiedades de navegación estén disponibles
                if (notifiacion.IdCitaNavigation?.IdAfiliadoNavigation != null)
                {
                    var email = notifiacion.IdCitaNavigation.IdAfiliadoNavigation.Email;

                    // Serializar el mensaje y publicar el evento
                    var messageSerialize = JsonSerializer.Serialize(notifiacion.Mensaje);
                    PublishEvent("NotificacionCita", messageSerialize, email);

                    // Redirigir a Index después de la creación
                    return RedirectToAction(nameof(Index));
                }
            }
            ViewData["IdCita"] = new SelectList(_context.CitaMedicas, "IdCita", "IdCita", notifiacion.IdCita);
            return View(notifiacion);
        }

        // GET: Notifiacion/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notifiacion = await _context.Notifiacions.FindAsync(id);
            if (notifiacion == null)
            {
                return NotFound();
            }
            ViewData["IdCita"] = new SelectList(_context.CitaMedicas, "IdCita", "IdCita", notifiacion.IdCita);
            return View(notifiacion);
        }

        // POST: Notifiacion/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdRecordatorio,IdCita,FechaEnvio,Mensaje,Enviada")] Notifiacion notifiacion)
        {
            if (id != notifiacion.IdRecordatorio)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(notifiacion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NotifiacionExists(notifiacion.IdRecordatorio))
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
            ViewData["IdCita"] = new SelectList(_context.CitaMedicas, "IdCita", "IdCita", notifiacion.IdCita);
            return View(notifiacion);
        }

        // GET: Notifiacion/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var notifiacion = await _context.Notifiacions
                .Include(n => n.IdCitaNavigation)
                .FirstOrDefaultAsync(m => m.IdRecordatorio == id);
            if (notifiacion == null)
            {
                return NotFound();
            }

            return View(notifiacion);
        }

        // POST: Notifiacion/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var notifiacion = await _context.Notifiacions.FindAsync(id);
            if (notifiacion != null)
            {
                _context.Notifiacions.Remove(notifiacion);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NotifiacionExists(int id)
        {
            return _context.Notifiacions.Any(e => e.IdRecordatorio == id);
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
