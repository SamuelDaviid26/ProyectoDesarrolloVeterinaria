namespace Veterinaria.Api.Models;

public class Veterinario
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string NumeroColegiado { get; set; } = string.Empty;
    public int? FotoId { get; set; }
    public Archivo? Foto { get; set; }
}
