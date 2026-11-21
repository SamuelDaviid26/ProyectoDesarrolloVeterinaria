namespace Veterinaria.Api.Models;

public class VacunaAplicada
{
    public int Id { get; set; }
    public int MascotaId { get; set; }
    public string NombreVacuna { get; set; } = string.Empty;
    public DateOnly FechaAplicacion { get; set; }
    public DateOnly? FechaProximaDosis { get; set; }
    public DateTimeOffset RegistradoEn { get; set; }
    public int? VacunaAnteriorId { get; set; }
    public int VeterinarioResponsableId { get; set; }
    public string RegistradoPorId { get; set; } = string.Empty;
    public Mascota? Mascota { get; set; }
    public VacunaAplicada? VacunaAnterior { get; set; }
    public Veterinario? VeterinarioResponsable { get; set; }
    public Usuario? RegistradoPor { get; set; }
}
