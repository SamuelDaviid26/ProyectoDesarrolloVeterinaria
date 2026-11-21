using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs.Veterinarios;

public abstract class VeterinarioRequestBase
{
    private string nombreCompleto = string.Empty;
    private string numeroColegiado = string.Empty;

    [Required]
    [StringLength(200)]
    public string NombreCompleto
    {
        get => nombreCompleto;
        set => nombreCompleto = value?.Trim() ?? string.Empty;
    }

    [Required]
    [StringLength(50)]
    public string NumeroColegiado
    {
        get => numeroColegiado;
        set => numeroColegiado = value?.Trim() ?? string.Empty;
    }
}
