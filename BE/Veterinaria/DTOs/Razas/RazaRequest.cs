using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs.Razas;

public class RazaRequest
{
    private string nombre = string.Empty;

    [Required]
    [StringLength(100)]
    public string Nombre
    {
        get => nombre;
        set => nombre = value?.Trim() ?? string.Empty;
    }

    public int EspecieId { get; set; }
}
