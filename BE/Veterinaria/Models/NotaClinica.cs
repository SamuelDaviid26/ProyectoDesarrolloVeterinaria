namespace Veterinaria.Api.Models;

public class NotaClinica
{
    public int Id { get; set; }
    public int MascotaId { get; set; }
    public string ContenidoHtml { get; set; } = string.Empty;
    public DateTimeOffset RegistradoEn { get; set; }
    public int VeterinarioResponsableId { get; set; }
    public string RegistradoPorId { get; set; } = string.Empty;
    public Mascota? Mascota { get; set; }
    public Veterinario? VeterinarioResponsable { get; set; }
    public Usuario? RegistradoPor { get; set; }
}
