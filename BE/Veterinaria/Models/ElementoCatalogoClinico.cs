namespace Veterinaria.Api.Models;

public class ElementoCatalogoClinico
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public TipoElementoClinico Tipo { get; set; }
}
