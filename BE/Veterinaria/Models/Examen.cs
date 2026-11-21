namespace Veterinaria.Api.Models;

public class Examen
{
    public int Id { get; set; }
    public int MascotaId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int ArchivoId { get; set; }
    public DateTimeOffset RegistradoEn { get; set; }
    public int VeterinarioResponsableId { get; set; }
    public string RegistradoPorId { get; set; } = string.Empty;
    public Mascota? Mascota { get; set; }
    public Archivo? Archivo { get; set; }
    public Veterinario? VeterinarioResponsable { get; set; }
    public Usuario? RegistradoPor { get; set; }
}
