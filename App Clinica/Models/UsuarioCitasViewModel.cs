namespace App_Clinica.Models
{
    public class UsuarioCitasViewModel
    {
        public Usuario Usuario { get; set; }
        public IEnumerable<CitaMedica> CitasMedicas { get; set; }
    }
}
