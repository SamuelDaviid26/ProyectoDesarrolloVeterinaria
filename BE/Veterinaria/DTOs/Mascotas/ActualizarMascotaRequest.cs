using Microsoft.AspNetCore.Http;

namespace Veterinaria.Api.DTOs.Mascotas;

public class ActualizarMascotaRequest : MascotaRequestBase
{
    public IFormFile? Foto { get; set; }
}
