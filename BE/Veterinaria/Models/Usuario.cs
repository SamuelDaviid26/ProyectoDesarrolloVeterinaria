using Microsoft.AspNetCore.Identity;

namespace Veterinaria.Api.Models;

public class Usuario : IdentityUser
{
    public string Cedula { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public bool Bloqueado { get; set; }
    public int? VeterinarioId { get; set; }
    public Veterinario? Veterinario { get; set; }
}
