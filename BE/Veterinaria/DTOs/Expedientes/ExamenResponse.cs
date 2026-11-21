namespace Veterinaria.Api.DTOs.Expedientes;

public sealed record ExamenResponse(
    int Id, string Descripcion, DateTimeOffset RegistradoEn, int ArchivoId, string ArchivoNombre,
    string ArchivoTipoContenido, string ArchivoUrl, int VeterinarioResponsableId,
    string VeterinarioResponsableNombre, string RegistradoPorId, string RegistradoPorNombre);
