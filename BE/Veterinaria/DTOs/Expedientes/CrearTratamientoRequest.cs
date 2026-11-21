using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs.Expedientes;

public class CrearTratamientoRequest
{
    [Range(1, int.MaxValue)]
    public int ElementoCatalogoClinicoId { get; set; }
    public int? VeterinarioResponsableId { get; set; }
}
