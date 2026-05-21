-- Base de datos
CREATE DATABASE NotificacionClinica;
GO

USE NotificacionClinica;
GO

-- Tabla para tipos de usuarios
CREATE TABLE TipoUsuario (
    IdTipoUsuario INT IDENTITY(1,1) PRIMARY KEY,
    NombreTipo NVARCHAR(20) NOT NULL CHECK (NombreTipo IN ('Afiliado', 'Personal Médico')),
);
GO

-- Tabla de usuarios (afiliados y personal médico)
CREATE TABLE Usuario (
    IdUsuario INT IDENTITY(1,1) PRIMARY KEY,
    IdTipoUsuario INT NOT NULL FOREIGN KEY REFERENCES TipoUsuario(IdTipoUsuario),
    NumeroIdentificacion NVARCHAR(20) NOT NULL UNIQUE,
    Contraseña VARBINARY(MAX) NOT NULL,
    Nombre NVARCHAR(100) NOT NULL,
    Apellido NVARCHAR(100) NOT NULL,
	Especialidad NVARCHAR(100) NOT NULL,
	Disponibilidad NVARCHAR(20) NOT NULL CHECK (Disponibilidad IN ('Disponible', 'No Disponible')),
    Email NVARCHAR(100) UNIQUE NOT NULL,
    Telefono NVARCHAR(15) NULL,
    FechaRegistro DATETIME DEFAULT GETDATE()
);
GO

-- Tabla de citas médicas
CREATE TABLE CitaMedica (
    IdCita INT IDENTITY(1,1) PRIMARY KEY,
    IdAfiliado INT NOT NULL FOREIGN KEY REFERENCES Usuario(IdUsuario),
    IdPersonalMedico INT NOT NULL FOREIGN KEY REFERENCES Usuario(IdUsuario),
    FechaCita DATETIME NOT NULL,
    Estado NVARCHAR(20) NOT NULL CHECK (Estado IN ('Pendiente', 'Completada', 'Cancelada')),
    Comentarios NVARCHAR(MAX) NULL
);
GO

-- Tabla para recordatorios
CREATE TABLE Notifiacion (
    IdRecordatorio INT IDENTITY(1,1) PRIMARY KEY,
    IdCita INT NOT NULL FOREIGN KEY REFERENCES CitaMedica(IdCita),
    FechaEnvio DATETIME NOT NULL DEFAULT GETDATE(),
    Mensaje NVARCHAR(500) NOT NULL,
	Enviada BIT NOT NULL DEFAULT 0
);
GO

ALTER TABLE Usuario
ALTER COLUMN Contraseña NVARCHAR(MAX) NOT NULL;

-- Cambiar la columna 'Especialidad' para permitir valores NULL
ALTER TABLE Usuario
ALTER COLUMN Especialidad NVARCHAR(100) NULL;

-- Cambiar la columna 'Disponibilidad' para permitir valores NULL
ALTER TABLE Usuario
ALTER COLUMN Disponibilidad NVARCHAR(20) NULL;
