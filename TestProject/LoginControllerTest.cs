using App_Clinica.Controllers;
using App_Clinica.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject
{
    public class LoginControllerTest
    {
        [Fact]
        public async Task Login_Post_ValidCredentials_ShouldRedirectToUsuarioIndex()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<NotificacionClinicaContext>()
                .UseInMemoryDatabase("LoginTestDB")
                .EnableSensitiveDataLogging()
                .Options;

            var tipoUsuario = new TipoUsuario
            {
                IdTipoUsuario = 1,
                NombreTipo = "Medico"
            };

            var usuario = new Usuario
            {
                IdUsuario = 1,
                IdTipoUsuario = 1,
                NumeroIdentificacion = "12345",
                Contraseña = "abc",
                Nombre = "Juan",
                Apellido = "Pérez",
                Email = "juan@example.com",
                Telefono = "123456789",
                FechaRegistro = DateTime.Now,
                IdTipoUsuarioNavigation = tipoUsuario
            };

            // Poblar BD InMemory
            using (var context = new NotificacionClinicaContext(options))
            {
                context.TipoUsuarios.Add(tipoUsuario);
                context.Usuarios.Add(usuario);
                await context.SaveChangesAsync();
            }

            using (var context = new NotificacionClinicaContext(options))
            {
                var controller = new LoginController(context);

                // Mock IAuthenticationService para HttpContext.SignInAsync
                var authServiceMock = new Mock<IAuthenticationService>();
                authServiceMock
                    .Setup(x => x.SignInAsync(
                        It.IsAny<HttpContext>(),
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                        It.IsAny<AuthenticationProperties>()))
                    .Returns(Task.CompletedTask);

                var serviceProviderMock = new Mock<IServiceProvider>();
                serviceProviderMock.Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                                   .Returns(authServiceMock.Object);

                var httpContext = new DefaultHttpContext
                {
                    RequestServices = serviceProviderMock.Object
                };

                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                };

                // Mock UrlHelper para evitar error en RedirectToAction
                var urlHelperMock = new Mock<IUrlHelper>();
                urlHelperMock.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("/Usuario/Index");
                controller.Url = urlHelperMock.Object;

                var loginModel = new Login
                {
                    NumeroIdentificacion = "12345",
                    Contraseña = "abc"
                };

                // Act
                var result = await controller.login(loginModel);

                // Assert
                var redirectResult = Assert.IsType<RedirectToActionResult>(result);
                Assert.Equal("Index", redirectResult.ActionName);
                Assert.Equal("Usuario", redirectResult.ControllerName);
            }
        }

        [Fact]
        public async Task Logout_ShouldSignOutAndRedirectToInicio()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<NotificacionClinicaContext>()
                .UseInMemoryDatabase("LogoutTestDB")
                .Options;

            using (var context = new NotificacionClinicaContext(options))
            {
                var controller = new LoginController(context);

                var authServiceMock = new Mock<IAuthenticationService>();
                authServiceMock
                    .Setup(x => x.SignOutAsync(
                        It.IsAny<HttpContext>(),
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        null))
                    .Returns(Task.CompletedTask);

                var serviceProviderMock = new Mock<IServiceProvider>();
                serviceProviderMock.Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                                   .Returns(authServiceMock.Object);

                var httpContext = new DefaultHttpContext
                {
                    RequestServices = serviceProviderMock.Object
                };

                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                };

                var urlHelperMock = new Mock<IUrlHelper>();
                controller.Url = urlHelperMock.Object;

                // Act
                var result = await controller.Logout();

                // Assert
                var redirectResult = Assert.IsType<RedirectToActionResult>(result);
                Assert.Equal("Inicio", redirectResult.ActionName);
                Assert.Equal("Usuario", redirectResult.ControllerName);
            }
        }
    }
}
