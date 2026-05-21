using System;
using System.Collections.Generic;

namespace App_Clinica.Models;

public partial class CitaMedica
{
    public int IdCita { get; set; }

    public int IdAfiliado { get; set; }

    public int IdPersonalMedico { get; set; }

    public DateTime FechaCita { get; set; }

    public string Estado { get; set; } = null!;

    public string? Comentarios { get; set; }

    public virtual Usuario? IdAfiliadoNavigation { get; set; }

    public virtual Usuario? IdPersonalMedicoNavigation { get; set; }

    public virtual ICollection<Notifiacion> Notifiacions { get; set; } = new List<Notifiacion>();
}
