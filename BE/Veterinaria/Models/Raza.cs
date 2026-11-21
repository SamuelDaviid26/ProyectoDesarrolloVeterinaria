namespace Veterinaria.Api.Models;

public class Raza
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int EspecieId { get; set; }
    public Especie? Especie { get; set; }
}
