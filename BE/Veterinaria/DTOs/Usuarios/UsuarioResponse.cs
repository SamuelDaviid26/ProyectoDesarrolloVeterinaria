namespace Veterinaria.Api.DTOs.Usuarios;

public sealed record UsuarioResponse(
    string Id,
    string Cedula,
    string NombreCompleto,
    string Correo,
    string Rol,
    int? VeterinarioId);
