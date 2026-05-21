using System;
using System.Collections.Generic;

namespace App_Clinica.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public int IdTipoUsuario { get; set; }

    public string NumeroIdentificacion { get; set; } = null!;

    public string Contraseña { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string? Especialidad { get; set; }

    public string? Disponibilidad { get; set; }

    public string Email { get; set; } = null!;

    public string? Telefono { get; set; } = null!;

    public DateTime? FechaRegistro { get; set; }

    public virtual ICollection<CitaMedica> CitaMedicaIdAfiliadoNavigations { get; set; } = new List<CitaMedica>();

    public virtual ICollection<CitaMedica> CitaMedicaIdPersonalMedicoNavigations { get; set; } = new List<CitaMedica>();

    public virtual TipoUsuario? IdTipoUsuarioNavigation { get; set; }
}
