namespace Veterinaria.Api.DTOs.Expedientes;

public sealed record TratamientoResponse(
    int Id, int ElementoCatalogoClinicoId, string Nombre, string Descripcion, string Tipo,
    DateTimeOffset AsignadoEn, int VeterinarioResponsableId, string VeterinarioResponsableNombre,
    string RegistradoPorId, string RegistradoPorNombre, DateTimeOffset? SuspendidoEn,
    int? VeterinarioSuspensionResponsableId, string? VeterinarioSuspensionResponsableNombre,
    string? SuspendidoPorId, string? SuspendidoPorNombre);

public sealed record TratamientosResponse(
    IReadOnlyList<TratamientoResponse> Vigentes,
    IReadOnlyList<TratamientoResponse> Suspendidos);
