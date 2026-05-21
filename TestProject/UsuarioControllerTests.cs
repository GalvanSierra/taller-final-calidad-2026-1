

using App_Clinica.Controllers;
using App_Clinica.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace TestProject
{
    public class UsuarioControllerTests
    {
        //Retorna correctamente la vista con el modelo del usuario que está autenticado
        [Fact]
        public async Task Index_ReturnsViewResult_WithLoggedUser()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<NotificacionClinicaContext>()
                .UseInMemoryDatabase("TestDb_Index_ReturnsViewResult_WithLoggedUser")
                .EnableSensitiveDataLogging()
                .Options;

            // Agrega TipoUsuario
            var tipoUsuario = new TipoUsuario
            {
                IdTipoUsuario = 1,
                NombreTipo = "Medico"
            };

            var testUser = new Usuario
            {
                IdUsuario = 1,
                IdTipoUsuario = 1,
                NumeroIdentificacion = "12345", // Debe coincidir con el claim
                Contraseña = "test123",
                Nombre = "Test",
                Apellido = "User",
                Email = "test@example.com",
                Telefono = "123456",
                FechaRegistro = DateTime.Now,
                IdTipoUsuarioNavigation = tipoUsuario
            };

            // Pobla la BD
            using (var context = new NotificacionClinicaContext(options))
            {
                context.TipoUsuarios.Add(tipoUsuario);
                context.Usuarios.Add(testUser);
                await context.SaveChangesAsync();
            }

            using (var context = new NotificacionClinicaContext(options))
            {
                var controller = new UsuarioController(context);

                // Simula usuario autenticado
                var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "12345")
                   }, "mock"));

                controller.ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                };

                // Act
                var result = await controller.Index();

                // Assert
                var viewResult = Assert.IsType<ViewResult>(result);
                var model = Assert.IsAssignableFrom<Usuario>(viewResult.Model);
                Assert.Equal("12345", model.NumeroIdentificacion);
                Assert.Equal("Medico", model.IdTipoUsuarioNavigation?.NombreTipo);
            }
        }

        //Cuando se crea un nuevo usuario con datos válidos
        [Fact]
        public async Task Create_ValidModel_RedirectsToLogin()
        {
            var options = new DbContextOptionsBuilder<NotificacionClinicaContext>()
                .UseInMemoryDatabase("CreateUserTestDb")
                .Options;

            using var context = new NotificacionClinicaContext(options);

            var controller = new UsuarioController(context);

            var usuario = new Usuario
            {
                IdUsuario = 2,
                IdTipoUsuario = 1,
                NumeroIdentificacion = "54321",
                Contraseña = "password",
                Nombre = "Nuevo",
                Apellido = "Usuario",
                Email = "nuevo@example.com",
                Telefono = "123456",
                FechaRegistro = System.DateTime.Now
            };

            var result = await controller.Create(usuario);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("login", redirectResult.ActionName);
            Assert.Equal("Login", redirectResult.ControllerName);
        }
    }
}