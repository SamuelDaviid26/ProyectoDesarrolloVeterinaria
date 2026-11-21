namespace Veterinaria.Api.DTOs.Veterinarios;

public sealed record VeterinarioResponse(
    int Id,
    string NombreCompleto,
    string NumeroColegiado,
    string? FotoUrl);
