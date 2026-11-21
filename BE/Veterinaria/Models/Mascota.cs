namespace Veterinaria.Api.Models;

public class Mascota
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int RazaId { get; set; }
    public DateOnly FechaNacimiento { get; set; }
    public string Sexo { get; set; } = string.Empty;
    public int? FotoId { get; set; }
    public string DuenoId { get; set; } = string.Empty;
    public Raza? Raza { get; set; }
    public Archivo? Foto { get; set; }
    public Usuario? Dueno { get; set; }
}
