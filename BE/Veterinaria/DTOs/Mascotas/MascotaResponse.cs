namespace Veterinaria.Api.DTOs.Mascotas;

public sealed record MascotaResponse(
    int Id, string Nombre, int RazaId, string RazaNombre, int EspecieId, string EspecieNombre,
    DateOnly FechaNacimiento, string Sexo, string DuenoId, string DuenoNombreCompleto,
    string DuenoCedula, string? FotoUrl);
