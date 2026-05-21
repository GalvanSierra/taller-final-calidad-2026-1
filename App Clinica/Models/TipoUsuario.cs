using System;
using System.Collections.Generic;

namespace App_Clinica.Models;

public partial class TipoUsuario
{
    public int IdTipoUsuario { get; set; }

    public string NombreTipo { get; set; } = null!;

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
