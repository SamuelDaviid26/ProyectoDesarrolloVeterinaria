using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Veterinaria.Api.DTOs.Expedientes;

public class CrearExamenRequest
{
    [Required, StringLength(1000)]
    public string Descripcion { get; set; } = string.Empty;
    [Required]
    public IFormFile? Archivo { get; set; }
    public int? VeterinarioResponsableId { get; set; }
}
