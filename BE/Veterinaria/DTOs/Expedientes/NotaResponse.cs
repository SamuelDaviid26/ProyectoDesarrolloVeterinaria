namespace Veterinaria.Api.DTOs.Expedientes;

public sealed record NotaResponse(
    int Id, string ContenidoHtml, DateTimeOffset RegistradoEn,
    int VeterinarioResponsableId, string VeterinarioResponsableNombre,
    string RegistradoPorId, string RegistradoPorNombre);
