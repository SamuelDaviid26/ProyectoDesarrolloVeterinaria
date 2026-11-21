using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs.Expedientes;

public class CrearNotaRequest
{
    [Required]
    public string ContenidoHtml { get; set; } = string.Empty;
    public int? VeterinarioResponsableId { get; set; }
}
