namespace Veterinaria.Api.DTOs.Razas;

public sealed record RazaResponse(int Id, string Nombre, int EspecieId, string EspecieNombre);
