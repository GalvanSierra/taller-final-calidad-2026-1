using App_Clinica.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Text;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;
using RabbitMQ.Client;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace App_Clinica.Controllers
{
    public class CitaMedicaController : Controller
    {
        private readonly NotificacionClinicaContext _context;

        private readonly NotifiacionController _notificacionController;

        public CitaMedicaController(NotificacionClinicaContext context, NotifiacionController notificacionController)
        {
            _context = context;
            _notificacionController = notificacionController;
        }

        // GET: CitaMedica
        public async Task<IActionResult> Index(Notifiacion notifiacion)
        {
            //var notificacionClinicaContext = _context.CitaMedicas.Include(c => c.IdAfiliadoNavigation).Include(c => c.IdPersonalMedicoNavigation);
            //return View(await notificacionClinicaContext.ToListAsync());

            var numeroIdentificacion = User.Identity.Name;

            // Obtener el usuario logueado
            var usuarioLogueado = await _context.Usuarios
                .Include(u => u.IdTipoUsuarioNavigation)
                .FirstOrDefaultAsync(u => u.NumeroIdentificacion == numeroIdentificacion);

            // Obtener las citas asociadas al usuario
            var citasMedicas = await _context.CitaMedicas
                .Include(c => c.IdAfiliadoNavigation)
                .Include(c => c.IdPersonalMedicoNavigation)
                .Where(c => c.IdAfiliado == usuarioLogueado.IdUsuario || c.IdPersonalMedico == usuarioLogueado.IdUsuario) // Ajusta según la relación
                .ToListAsync();

            if (usuarioLogueado.IdTipoUsuario.Equals(2))
            {
                citasMedicas.ForEach(c => {
                    if ((c.FechaCita.Date - DateTime.Now.Date).TotalDays == 1)
                    {

                        var notificacion = new Notifiacion
                        {
                            IdCita = c.IdCita,
                            FechaEnvio = DateTime.Now,
                            Mensaje = $"Hola {c.IdAfiliadoNavigation.Nombre.ToString()}, recuerda que tienes una cita asignada para el dia de manana con el medico {c.IdPersonalMedicoNavigation.Nombre.ToString()}",
                            Enviada = true
                        };

                        _notificacionController.Create(notificacion).Wait();
                    }
                });
            }
            
            // Crear el ViewModel
            var viewModel = new UsuarioCitasViewModel
            {
                Usuario = usuarioLogueado,
                CitasMedicas = citasMedicas
            };

            return View(viewModel);
        }

        // GET: CitaMedica/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var citaMedica = await _context.CitaMedicas
                .Include(c => c.IdAfiliadoNavigation)
                .Include(c => c.IdPersonalMedicoNavigation)
                .FirstOrDefaultAsync(m => m.IdCita == id);
            if (citaMedica == null)
            {
                return NotFound();
            }

            return View(citaMedica);
        }

        // GET: CitaMedica/Create
        public IActionResult Create()
        {
            var numeroIdentificacion = User.Identity.Name;

            // Crear la lista de afiliados (usuarios con IdTipoUsuario == 1)
            var usuariosAfiliados = _context.Usuarios
                .Where(u => u.IdTipoUsuario == 1 && u.NumeroIdentificacion == numeroIdentificacion) // Filtrar por IdTipoUsuario == 1
                .Select(u => new SelectListItem
                {
                    Value = u.IdUsuario.ToString(),  // El valor será el IdUsuario
                    Text = u.Nombre + " " + u.Apellido // El texto será el Nombre y Apellido combinados
                })
                .ToList();

            // Crear la lista de médicos (usuarios con IdTipoUsuario == 2)
            var personalMedico = _context.Usuarios
                .Where(u => u.IdTipoUsuario == 2) // Filtrar por IdTipoUsuario == 2
                .Select(u => new SelectListItem
                {
                    Value = u.IdUsuario.ToString(),
                    Text = u.Nombre + " " + u.Apellido
                })
                .ToList();

            // Pasar las listas a ViewBag para usarlas en la vista
            ViewBag.IdAfiliado = usuariosAfiliados;
            ViewBag.IdPersonalMedico = personalMedico;

            return View();
        }

        // POST: CitaMedica/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdCita,IdAfiliado,IdPersonalMedico,FechaCita,Estado,Comentarios")] CitaMedica citaMedica)
        {
            var numeroIdentificacion = User.Identity.Name;

            // Obtener el usuario logueado
            var usuarioLogueado = await _context.Usuarios
                .Include(u => u.IdTipoUsuarioNavigation)
                .FirstOrDefaultAsync(u => u.NumeroIdentificacion == numeroIdentificacion);

            var email = usuarioLogueado.Email;
            var nombre = usuarioLogueado.Nombre;

            if (ModelState.IsValid)
            {
                var medico = _context.Usuarios
                    .FirstOrDefault(u => u.IdUsuario == citaMedica.IdPersonalMedico);

                var nombremedico = medico.Nombre;

                _context.Add(citaMedica);
                await _context.SaveChangesAsync();

                // Publicar evento
                var fechaCitaFormateada = citaMedica.FechaCita.ToString("dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture);
                var message = $"Hola {nombre}, tu cita ha sido agendada exitosamente para la fecha {fechaCitaFormateada}, con el medico {nombremedico}";
                var messageSerialize = JsonSerializer.Serialize(message);
                PublishEvent("CitaCreada", messageSerialize, email);

                return RedirectToAction(nameof(Index));
            }
            ViewData["IdAfiliado"] = new SelectList(_context.Usuarios, "IdUsuario", "IdUsuario", citaMedica.IdAfiliado);
            ViewData["IdPersonalMedico"] = new SelectList(_context.Usuarios, "IdUsuario", "IdUsuario", citaMedica.IdPersonalMedico);
            return View(citaMedica);
        }

        // GET: CitaMedica/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var citaMedica = await _context.CitaMedicas.FindAsync(id);
            if (citaMedica == null)
            {
                return NotFound();
            }
            ViewData["IdAfiliado"] = new SelectList(_context.Usuarios, "IdUsuario", "IdUsuario", citaMedica.IdAfiliado);
            ViewData["IdPersonalMedico"] = new SelectList(_context.Usuarios, "IdUsuario", "IdUsuario", citaMedica.IdPersonalMedico);
            return View(citaMedica);
        }

        // POST: CitaMedica/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdCita,IdAfiliado,IdPersonalMedico,FechaCita,Estado,Comentarios")] CitaMedica citaMedica)
        {
            if (id != citaMedica.IdCita)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(citaMedica);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CitaMedicaExists(citaMedica.IdCita))
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
            ViewData["IdAfiliado"] = new SelectList(_context.Usuarios, "IdUsuario", "IdUsuario", citaMedica.IdAfiliado);
            ViewData["IdPersonalMedico"] = new SelectList(_context.Usuarios, "IdUsuario", "IdUsuario", citaMedica.IdPersonalMedico);
            return View(citaMedica);
        }

        // GET: CitaMedica/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var citaMedica = await _context.CitaMedicas
                .Include(c => c.IdAfiliadoNavigation)
                .Include(c => c.IdPersonalMedicoNavigation)
                .FirstOrDefaultAsync(m => m.IdCita == id);
            if (citaMedica == null)
            {
                return NotFound();
            }

            return View(citaMedica);
        }

        // POST: CitaMedica/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var citaMedica = await _context.CitaMedicas.FindAsync(id);
            if (citaMedica != null)
            {
                _context.CitaMedicas.Remove(citaMedica);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CitaMedicaExists(int id)
        {
            return _context.CitaMedicas.Any(e => e.IdCita == id);
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
