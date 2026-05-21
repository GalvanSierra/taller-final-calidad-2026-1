# 🏥 App Clínica — Sistema de Gestión de Citas Médicas

Aplicación web para la gestión de citas médicas, notificaciones y usuarios de una EPS. Desarrollada con ASP.NET Core 9 MVC, Entity Framework Core y mensajería asíncrona con RabbitMQ.

---

## 📋 Tabla de contenido

- [🏥 App Clínica — Sistema de Gestión de Citas Médicas](#-app-clínica--sistema-de-gestión-de-citas-médicas)
  - [📋 Tabla de contenido](#-tabla-de-contenido)
  - [Descripción](#descripción)
  - [Tecnologías](#tecnologías)
  - [Arquitectura](#arquitectura)
  - [Requisitos previos](#requisitos-previos)
  - [Configuración](#configuración)
    - [1. Cadena de conexión](#1-cadena-de-conexión)
    - [2. Base de datos](#2-base-de-datos)
    - [3. Gmail API (OAuth 2.0)](#3-gmail-api-oauth-20)
    - [4. RabbitMQ](#4-rabbitmq)
  - [Ejecución](#ejecución)
  - [Estructura del proyecto](#estructura-del-proyecto)
  - [Funcionalidades](#funcionalidades)
    - [Autenticación](#autenticación)
    - [Gestión de usuarios](#gestión-de-usuarios)
    - [Citas médicas](#citas-médicas)
    - [Notificaciones por correo](#notificaciones-por-correo)
  - [Pruebas](#pruebas)
  - [CI/CD](#cicd)
  - [Seguridad](#seguridad)

---

## Descripción

App Clínica permite a los usuarios (afiliados y personal médico) gestionar citas médicas en línea. Cuando se agenda una cita o se crea un registro, el sistema envía notificaciones automáticas por correo electrónico a través de la API de Gmail, utilizando RabbitMQ como broker de mensajería para desacoplar el envío de correos del flujo principal de la aplicación.

---

## Tecnologías

| Capa          | Tecnología                       |
| ------------- | -------------------------------- |
| Framework web | ASP.NET Core 9 MVC               |
| ORM           | Entity Framework Core 9          |
| Base de datos | SQL Server                       |
| Mensajería    | RabbitMQ (exchange tipo `topic`) |
| Correo        | Google Gmail API v1 (OAuth 2.0)  |
| Logging       | Serilog (consola + archivo)      |
| Pruebas       | xUnit + Moq + EF Core InMemory   |
| CI/CD         | Azure Pipelines                  |

---

## Arquitectura

```
┌─────────────────────────────────────────────┐
│              ASP.NET Core MVC               │
│  Controllers → Models → Views (Razor)       │
└────────────────────┬────────────────────────┘
                     │ publica eventos
                     ▼
┌─────────────────────────────────────────────┐
│         RabbitMQ — Exchange: clinic_events  │
│  Colas: queue_citamedica                    │
│         queue_registro                      │
│         queue_notificacioncita              │
└────────────────────┬────────────────────────┘
                     │ consume
                     ▼
┌─────────────────────────────────────────────┐
│         EventConsumerService                │
│         (BackgroundService)                 │
│         → Gmail API → correo al usuario     │
└─────────────────────────────────────────────┘
```

**Roles de usuario:**

- **Afiliado (IdTipoUsuario = 1):** puede crear y consultar sus citas médicas.
- **Personal médico (IdTipoUsuario = 2):** puede editar, gestionar citas y ver notificaciones.

---

## Requisitos previos

- [.NET SDK 9.0](https://dotnet.microsoft.com/download)
- SQL Server (local o remoto)
- [RabbitMQ](https://www.rabbitmq.com/download.html) corriendo en `localhost:5672`
- Cuenta de Google con acceso a la API de Gmail
- `credentials.json` de Google Cloud Console (ver sección [Configuración](#configuración))

---

## Configuración

### 1. Cadena de conexión

Edita `App Clinica/appsettings.json` con los datos de tu servidor SQL:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=TU_SERVIDOR;Database=NotificacionClinica;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True;"
}
```

> ⚠️ No subas credenciales reales al repositorio. Usa variables de entorno o `appsettings.Production.json` (excluido por `.gitignore`).

### 2. Base de datos

Aplica las migraciones de Entity Framework:

```bash
cd "App Clinica"
dotnet ef database update
```

### 3. Gmail API (OAuth 2.0)

1. Ve a [Google Cloud Console](https://console.cloud.google.com/) y crea un proyecto.
2. Habilita la **Gmail API**.
3. Crea credenciales de tipo **OAuth 2.0 para aplicación de escritorio**.
4. Descarga el archivo y renómbralo `credentials.json`.
5. Colócalo en la raíz de `App Clinica/` (está en `.gitignore`, no se sube al repo).
6. Al ejecutar la aplicación por primera vez, se abrirá el navegador para autorizar el acceso. El token se guardará en `token.json/`.

### 4. RabbitMQ

Asegúrate de tener RabbitMQ corriendo localmente. Con Docker:

```bash
docker run -d --hostname rabbit --name rabbitmq \
  -p 5672:5672 -p 15672:15672 \
  rabbitmq:3-management
```

---

## Ejecución

```bash
cd "App Clinica"
dotnet run
```

La aplicación estará disponible en:

- HTTP: `http://localhost:5062`
- HTTPS: `https://localhost:7075`

---

## Estructura del proyecto

```
/
├── App Clinica/
│   ├── Controllers/
│   │   ├── CitaMedicaController.cs     # CRUD de citas + publicación RabbitMQ
│   │   ├── LoginController.cs          # Autenticación con cookies
│   │   ├── NotifiacionController.cs    # Gestión de notificaciones
│   │   ├── UsuarioController.cs        # Registro y perfil de usuarios
│   │   └── TipoUsuarioController.cs
│   ├── Consumidor de Eventos/
│   │   └── EventConsumerService.cs     # BackgroundService que escucha RabbitMQ
│   ├── Models/                         # Entidades EF Core y ViewModels
│   ├── Views/                          # Vistas Razor por controlador
│   ├── appsettings.json
│   └── App Clinica.csproj
├── TestProject/
│   ├── CitaMedicaControllerTests.cs
│   ├── LoginControllerTest.cs
│   └── TestProject.csproj
├── azure-pipelines.yml
└── .gitignore
```

---

## Funcionalidades

### Autenticación

- Login y logout con autenticación por cookies.
- Rutas protegidas según el rol del usuario.

### Gestión de usuarios

- Registro de afiliados y personal médico.
- Campos adicionales (especialidad, disponibilidad) visibles solo para médicos.

### Citas médicas

- Creación de citas por afiliados, seleccionando médico y fecha.
- Recordatorio automático si la cita es al día siguiente (notificación vía RabbitMQ).
- Edición y eliminación disponible para el personal médico.

### Notificaciones por correo

Los siguientes eventos disparan un correo al usuario:

| Evento               | Routing Key        | Cola                     |
| -------------------- | ------------------ | ------------------------ |
| Registro de usuario  | `RegistroCreado`   | `queue_registro`         |
| Cita agendada        | `CitaCreada`       | `queue_citamedica`       |
| Recordatorio de cita | `NotificacionCita` | `queue_notificacioncita` |

---

## Pruebas

El proyecto `TestProject` usa xUnit con base de datos en memoria para pruebas unitarias de los controladores.

```bash
cd TestProject
dotnet test
```

Pruebas incluidas:

- `CitaMedicaControllerTests` — verifica que `Index` retorne el ViewModel correcto con las citas del usuario autenticado.
- `LoginControllerTest` — verifica login exitoso con redirección, y logout con cierre de sesión.

---

## CI/CD

El archivo `azure-pipelines.yml` define un pipeline que:

1. Instala el SDK de .NET 9.
2. Ejecuta las pruebas xUnit del `TestProject`.
3. Genera el reporte `.trx` con los resultados.

El pipeline se activa con cada push a la rama `master`.

---

## Seguridad

- `credentials.json` y `token.json/` están en `.gitignore` y nunca deben subirse al repositorio.
- La cadena de conexión contiene credenciales: moverla a variables de entorno o a `appsettings.Production.json` antes de desplegar a producción.
- Las contraseñas de usuarios se almacenan en texto plano actualmente — se recomienda migrar a hashing con `BCrypt` o `ASP.NET Core Identity` antes de un despliegue en producción.
