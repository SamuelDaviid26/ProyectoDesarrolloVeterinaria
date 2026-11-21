using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs.Usuarios;

public class CambiarBloqueoRequest
{
    [Required]
    public bool? Bloqueado { get; set; }
}
