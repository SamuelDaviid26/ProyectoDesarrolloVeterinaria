using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Veterinaria.Api.DTOs.Veterinarios;

public class CrearVeterinarioRequest : VeterinarioRequestBase
{
    [Required]
    public IFormFile? Foto { get; set; }
}
