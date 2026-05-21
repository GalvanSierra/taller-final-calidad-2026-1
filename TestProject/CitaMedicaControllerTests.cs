using App_Clinica.Controllers;
using App_Clinica.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace TestProject
{
    public class CitaMedicaControllerTests
    {
        private NotificacionClinicaContext GetInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<NotificacionClinicaContext>()
                .UseInMemoryDatabase(dbName)
                .EnableSensitiveDataLogging()
                .Options;
            return new NotificacionClinicaContext(options);
        }

        private ClaimsPrincipal GetUser(string numeroIdentificacion)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, numeroIdentificacion)
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
        }

        [Fact]
        public async Task Index_ReturnsViewWithViewModel()
        {
            // Arrange
            var context = GetInMemoryContext("IndexTestDB");
            var contextNoti = GetInMemoryContext("NotificacionDB"); // contexto para NotifiacionController

            var tipoMedico = new TipoUsuario { IdTipoUsuario = 2, NombreTipo = "Medico" };
            var tipoAfiliado = new TipoUsuario { IdTipoUsuario = 1, NombreTipo = "Afiliado" };

            var medico = new Usuario
            {
                IdUsuario = 1,
                NumeroIdentificacion = "med123",
                Nombre = "Medico",
                Apellido = "Uno",
                IdTipoUsuario = 2,
                IdTipoUsuarioNavigation = tipoMedico,
                Contraseña = "dummy",
                Email = "medico@clinica.com"
            };

            var afiliado = new Usuario
            {
                IdUsuario = 2,
                NumeroIdentificacion = "afi123",
                Nombre = "Afiliado",
                Apellido = "Dos",
                IdTipoUsuario = 1,
                IdTipoUsuarioNavigation = tipoAfiliado,
                Contraseña = "dummy",
                Email = "afiliado@clinica.com"
            };

            context.TipoUsuarios.AddRange(tipoMedico, tipoAfiliado);
            context.Usuarios.AddRange(medico, afiliado);
            context.CitaMedicas.Add(new CitaMedica
            {
                IdCita = 1,
                IdAfiliado = afiliado.IdUsuario,
                IdPersonalMedico = medico.IdUsuario,
                FechaCita = DateTime.Now.AddDays(2),
                Estado = "Agendada"
            });
            await context.SaveChangesAsync();

            // Creamos mock pasando el contexto necesario para construir NotifiacionController
            var notiControllerMock = new Mock<NotifiacionController>(contextNoti);
            var notificacionController = notiControllerMock.Object;

            var controller = new CitaMedicaController(context, notificacionController);

            // Simular usuario logueado (afiliado)
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetUser("afi123")
                }
            };

            // Act
            var result = await controller.Index(null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<UsuarioCitasViewModel>(viewResult.Model);
            Assert.Equal("Afiliado", model.Usuario.IdTipoUsuarioNavigation.NombreTipo);
            Assert.Single(model.CitasMedicas);
        }

    }
}
