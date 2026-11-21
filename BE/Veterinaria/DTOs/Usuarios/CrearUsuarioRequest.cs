using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs.Usuarios;

public class CrearUsuarioRequest
{
    private string cedula = string.Empty;
    private string nombreCompleto = string.Empty;
    private string correo = string.Empty;
    private string rol = string.Empty;

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

    [Required]
    [StringLength(256)]
    public string Rol
    {
        get => rol;
        set => rol = value?.Trim() ?? string.Empty;
    }

    public int? VeterinarioId { get; set; }
}
