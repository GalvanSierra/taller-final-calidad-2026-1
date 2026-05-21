using App_Clinica.Consumidor_de_Eventos;
using App_Clinica.Controllers;
using App_Clinica.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog para consola y archivo
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // Muestra los logs en la consola
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day) // Muestra los logs en un archivo
    .CreateLogger();

builder.Logging.ClearProviders(); // Eliminar los proveedores de logs por defecto
builder.Logging.AddSerilog(); // Agregar Serilog como proveedor de logs

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Login/login"; // Ruta de login
            options.AccessDeniedPath = "/Login/Logout"; // Ruta de acceso denegado
        });

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<NotificacionClinicaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<NotifiacionController>();

builder.Services.AddHostedService<EventConsumerService>();

var app = builder.Build();

// Configurar el middleware de autenticación
app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Usuario}/{action=Inicio}/{id?}")
    .WithStaticAssets();

app.Run();


//using App_Clinica.Consumidor_de_Eventos;
//using App_Clinica.Models;
//using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.EntityFrameworkCore;
//using RabbitMQ.Client;
//using Serilog;

//var builder = WebApplication.CreateBuilder(args);

//builder.Logging.AddConsole();

//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//        .AddCookie(options =>
//        {
//            options.LoginPath = "/Login/login"; // Ruta de login
//            options.AccessDeniedPath = "/Login/Logout"; // Ruta de acceso denegado
//        });

//// Add services to the container.
//builder.Services.AddControllersWithViews();

//builder.Services.AddDbContext<NotificacionClinicaContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddHostedService<EventConsumerService>();

//builder.Logging.AddConsole();

//var app = builder.Build();

//// Configurar el middleware de autenticación
//app.UseAuthentication();
//app.UseAuthorization();

//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseRouting();

//app.UseAuthorization();

//app.MapStaticAssets();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Usuario}/{action=Inicio}/{id?}")
//    .WithStaticAssets();


//app.Run();
