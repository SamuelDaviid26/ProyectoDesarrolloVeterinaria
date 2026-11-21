namespace Veterinaria.Api.Models;

public class Archivo
{
    public int Id { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string TipoContenido { get; set; } = string.Empty;
    public byte[] Contenido { get; set; } = [];
}
