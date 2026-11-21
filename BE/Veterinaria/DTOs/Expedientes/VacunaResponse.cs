namespace Veterinaria.Api.DTOs.Expedientes;

public sealed record VacunaResponse(
    int Id, string NombreVacuna, DateOnly FechaAplicacion, DateOnly? FechaProximaDosis,
    DateTimeOffset RegistradoEn, int? VacunaAnteriorId, int VeterinarioResponsableId,
    string VeterinarioResponsableNombre, string RegistradoPorId, string RegistradoPorNombre,
    string? EstadoProximaDosis);
