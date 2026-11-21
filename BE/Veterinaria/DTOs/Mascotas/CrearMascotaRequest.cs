using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Veterinaria.Api.DTOs.Mascotas;

public class CrearMascotaRequest : MascotaRequestBase
{
    [Required]
    public IFormFile? Foto { get; set; }
}
