namespace Veterinaria.Api.DTOs.Usuarios;

public sealed record UsuarioDetalleResponse(
    string Id,
    string Cedula,
    string NombreCompleto,
    string Correo,
    string Rol,
    bool Bloqueado,
    int? VeterinarioId);
