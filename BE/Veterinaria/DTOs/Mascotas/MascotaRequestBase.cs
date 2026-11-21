using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs.Mascotas;

public class MascotaRequestBase
{
    private string nombre = string.Empty;
    private string sexo = string.Empty;
    private string duenoId = string.Empty;

    [Required, StringLength(100)]
    public string Nombre { get => nombre; set => nombre = value?.Trim() ?? string.Empty; }
    [Range(1, int.MaxValue)]
    public int RazaId { get; set; }
    public DateOnly FechaNacimiento { get; set; }
    [Required, StringLength(30)]
    public string Sexo { get => sexo; set => sexo = value?.Trim() ?? string.Empty; }
    [Required]
    public string DuenoId { get => duenoId; set => duenoId = value?.Trim() ?? string.Empty; }
}
