using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs.CatalogoClinico;

public class CatalogoClinicoRequest
{
    private string nombre = string.Empty;
    private string descripcion = string.Empty;

    [Required]
    [StringLength(150)]
    public string Nombre
    {
        get => nombre;
        set => nombre = value?.Trim() ?? string.Empty;
    }

    [Required]
    [RegularExpression(@"\A(Medicamento|Tratamiento)\z")]
    public string Tipo { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Descripcion
    {
        get => descripcion;
        set => descripcion = value?.Trim() ?? string.Empty;
    }
}
