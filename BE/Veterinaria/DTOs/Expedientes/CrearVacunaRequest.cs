using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs.Expedientes;

public class CrearVacunaRequest
{
    [Required, StringLength(150)]
    public string NombreVacuna { get; set; } = string.Empty;
    public DateOnly FechaAplicacion { get; set; }
    public DateOnly? FechaProximaDosis { get; set; }
    public int? VacunaAnteriorId { get; set; }
    public int? VeterinarioResponsableId { get; set; }
}
