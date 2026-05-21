using System;
using System.Collections.Generic;

namespace App_Clinica.Models;

public partial class Notifiacion
{
    public int IdRecordatorio { get; set; }

    public int IdCita { get; set; }

    public DateTime FechaEnvio { get; set; }

    public string Mensaje { get; set; } = null!;

    public bool Enviada { get; set; }

    public virtual CitaMedica IdCitaNavigation { get; set; } = null!;
}
