namespace Pension65Api.DTOs
{
    public class UsuarioDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? NombreCompleto { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
    }
}
