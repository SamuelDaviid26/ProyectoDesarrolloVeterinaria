namespace Veterinaria.Api.Models;

public class AsignacionClinica
{
    public int Id { get; set; }
    public int MascotaId { get; set; }
    public int ElementoCatalogoClinicoId { get; set; }
    public DateTimeOffset AsignadoEn { get; set; }
    public int VeterinarioResponsableId { get; set; }
    public string RegistradoPorId { get; set; } = string.Empty;
    public DateTimeOffset? SuspendidoEn { get; set; }
    public int? VeterinarioSuspensionResponsableId { get; set; }
    public string? SuspendidoPorId { get; set; }
    public Mascota? Mascota { get; set; }
    public ElementoCatalogoClinico? ElementoCatalogoClinico { get; set; }
    public Veterinario? VeterinarioResponsable { get; set; }
    public Usuario? RegistradoPor { get; set; }
    public Veterinario? VeterinarioSuspensionResponsable { get; set; }
    public Usuario? SuspendidoPor { get; set; }
}
