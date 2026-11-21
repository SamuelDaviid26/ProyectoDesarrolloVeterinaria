using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.DTOs.Auth;
using Veterinaria.Api.DTOs.Usuarios;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Services;

public enum EstadoCreacionUsuario
{
    Creado,
    Conflicto,
    DatosInvalidos,
    Error
}

public sealed record ResultadoCreacionUsuario(
    EstadoCreacionUsuario Estado,
    Usuario? Usuario = null,
    IReadOnlyList<string>? Errores = null);

public class UsuariosService(
    UserManager<Usuario> userManager,
    VeterinariaDbContext dbContext,
    DuenosService duenosService)
{
    private const string RolAdministrador = "Administrador";
    private const string RolVeterinario = "Veterinario";
    private const string RolDueno = "Dueño";

    public async Task<ResultadoCreacionUsuario> CrearAsync(CrearUsuarioRequest request)
    {
        if (request.Rol is not (RolAdministrador or RolVeterinario or RolDueno))
        {
            return DatosInvalidos("El rol debe ser Administrador, Veterinario o Dueño.");
        }

        if (request.Rol == RolDueno)
        {
            if (request.VeterinarioId.HasValue)
            {
                return DatosInvalidos("Un dueño no puede tener una ficha profesional vinculada.");
            }

            var resultadoDueno = await duenosService.CrearAsync(new RegistroDuenoRequest
            {
                Cedula = request.Cedula,
                NombreCompleto = request.NombreCompleto,
                Correo = request.Correo,
                Contrasena = request.Contrasena
            });

            return resultadoDueno.Estado switch
            {
                EstadoCreacionDueno.Creado => new ResultadoCreacionUsuario(
                    EstadoCreacionUsuario.Creado, resultadoDueno.Usuario),
                EstadoCreacionDueno.Conflicto => new ResultadoCreacionUsuario(EstadoCreacionUsuario.Conflicto),
                EstadoCreacionDueno.DatosInvalidos => new ResultadoCreacionUsuario(
                    EstadoCreacionUsuario.DatosInvalidos, Errores: resultadoDueno.Errores),
                _ => new ResultadoCreacionUsuario(EstadoCreacionUsuario.Error)
            };
        }

        if (request.Rol == RolAdministrador && request.VeterinarioId.HasValue)
        {
            return DatosInvalidos("Un administrador no puede tener una ficha profesional vinculada.");
        }

        if (request.Rol == RolVeterinario)
        {
            if (!request.VeterinarioId.HasValue)
            {
                return DatosInvalidos("VeterinarioId es obligatorio para el rol Veterinario.");
            }

            if (!await dbContext.Veterinarios.AnyAsync(veterinario => veterinario.Id == request.VeterinarioId))
            {
                return DatosInvalidos("La ficha profesional indicada no existe.");
            }

            if (await dbContext.Users.AnyAsync(usuario => usuario.VeterinarioId == request.VeterinarioId))
            {
                return new ResultadoCreacionUsuario(EstadoCreacionUsuario.Conflicto);
            }
        }

        if (await userManager.FindByEmailAsync(request.Correo) is not null ||
            await dbContext.Users.AnyAsync(usuario => usuario.Cedula == request.Cedula))
        {
            return new ResultadoCreacionUsuario(EstadoCreacionUsuario.Conflicto);
        }

        var usuarioNuevo = new Usuario
        {
            Cedula = request.Cedula,
            NombreCompleto = request.NombreCompleto,
            Email = request.Correo,
            UserName = request.Correo,
            Bloqueado = false,
            VeterinarioId = request.Rol == RolVeterinario ? request.VeterinarioId : null
        };

        try
        {
            await using var transaccion = await dbContext.Database.BeginTransactionAsync();

            var resultadoCreacion = await userManager.CreateAsync(usuarioNuevo, request.Contrasena);
            if (!resultadoCreacion.Succeeded)
            {
                await transaccion.RollbackAsync();
                if (EsConflicto(resultadoCreacion))
                {
                    return new ResultadoCreacionUsuario(EstadoCreacionUsuario.Conflicto);
                }

                return new ResultadoCreacionUsuario(
                    EstadoCreacionUsuario.DatosInvalidos,
                    Errores: resultadoCreacion.Errors.Select(error => error.Description).ToArray());
            }

            var resultadoRol = await userManager.AddToRoleAsync(usuarioNuevo, request.Rol);
            if (!resultadoRol.Succeeded)
            {
                await transaccion.RollbackAsync();
                return new ResultadoCreacionUsuario(
                    EstadoCreacionUsuario.Error,
                    Errores: resultadoRol.Errors.Select(error => error.Description).ToArray());
            }

            await transaccion.CommitAsync();
            return new ResultadoCreacionUsuario(EstadoCreacionUsuario.Creado, usuarioNuevo);
        }
        catch (DbUpdateException)
        {
            if (await userManager.FindByEmailAsync(request.Correo) is not null ||
                await dbContext.Users.AnyAsync(usuario => usuario.Cedula == request.Cedula) ||
                request.VeterinarioId.HasValue && await dbContext.Users.AnyAsync(
                    usuario => usuario.VeterinarioId == request.VeterinarioId))
            {
                return new ResultadoCreacionUsuario(EstadoCreacionUsuario.Conflicto);
            }

            if (request.VeterinarioId.HasValue &&
                !await dbContext.Veterinarios.AnyAsync(veterinario => veterinario.Id == request.VeterinarioId))
            {
                return DatosInvalidos("La ficha profesional indicada ya no existe.");
            }

            throw;
        }
    }

    public static UsuarioResponse ARespuesta(Usuario usuario, string rol)
    {
        return new UsuarioResponse(
            usuario.Id,
            usuario.Cedula,
            usuario.NombreCompleto,
            usuario.Email!,
            rol,
            usuario.VeterinarioId);
    }

    private static ResultadoCreacionUsuario DatosInvalidos(string error)
    {
        return new ResultadoCreacionUsuario(EstadoCreacionUsuario.DatosInvalidos, Errores: [error]);
    }

    private static bool EsConflicto(IdentityResult resultado)
    {
        return resultado.Errors.Any(error =>
            error.Code is "DuplicateEmail" or "DuplicateUserName");
    }
}
