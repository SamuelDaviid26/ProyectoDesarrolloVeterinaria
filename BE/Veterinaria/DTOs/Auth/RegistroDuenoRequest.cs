using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs.Auth;

public class RegistroDuenoRequest
{
    private string cedula = string.Empty;
    private string nombreCompleto = string.Empty;
    private string correo = string.Empty;

    [Required]
    [StringLength(32)]
    public string Cedula
    {
        get => cedula;
        set => cedula = value?.Trim() ?? string.Empty;
    }

    [Required]
    public string NombreCompleto
    {
        get => nombreCompleto;
        set => nombreCompleto = value?.Trim() ?? string.Empty;
    }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Correo
    {
        get => correo;
        set => correo = value?.Trim() ?? string.Empty;
    }

    [Required]
    public string Contrasena { get; set; } = string.Empty;
}
