namespace Veterinaria.Api.DTOs.Mascotas;

public sealed record MiMascotaResponse(
    int Id, string Nombre, string Especie, string Raza, string? FotoUrl);
