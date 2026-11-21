using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs.Especies;

public class EspecieRequest
{
    private string nombre = string.Empty;

    [Required]
    [StringLength(100)]
    public string Nombre
    {
        get => nombre;
        set => nombre = value?.Trim() ?? string.Empty;
    }
}
