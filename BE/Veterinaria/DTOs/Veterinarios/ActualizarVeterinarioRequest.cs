using Microsoft.AspNetCore.Http;

namespace Veterinaria.Api.DTOs.Veterinarios;

public class ActualizarVeterinarioRequest : VeterinarioRequestBase
{
    public IFormFile? Foto { get; set; }
}
