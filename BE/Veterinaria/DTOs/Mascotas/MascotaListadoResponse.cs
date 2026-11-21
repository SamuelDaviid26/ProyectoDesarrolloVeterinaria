namespace Veterinaria.Api.DTOs.Mascotas;

public sealed record MascotaListadoResponse(
    int Id, string Nombre, string DuenoId, string DuenoNombreCompleto, string DuenoCedula,
    int EspecieId, string EspecieNombre, int RazaId, string RazaNombre,
    DateOnly FechaNacimiento, string? FotoUrl, DateTimeOffset? UltimaAtencion);

public sealed record MascotasListadoResponse(
    IReadOnlyList<MascotaListadoResponse> AtencionesRecientes,
    IReadOnlyList<MascotaListadoResponse> SinAtenciones);
