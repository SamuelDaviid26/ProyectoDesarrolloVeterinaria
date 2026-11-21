using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;
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

    public async Task<IReadOnlyList<UsuarioDetalleResponse>> ListarAsync(
        string? nombre, string? cedula, string? correo, string? rol, bool? bloqueado)
    {
        var consulta = dbContext.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(nombre)) consulta = consulta.Where(u => u.NombreCompleto.Contains(nombre.Trim()));
        if (!string.IsNullOrWhiteSpace(cedula)) consulta = consulta.Where(u => u.Cedula.Contains(cedula.Trim()));
        if (!string.IsNullOrWhiteSpace(correo)) consulta = consulta.Where(u => u.Email != null && u.Email.Contains(correo.Trim()));
        if (bloqueado.HasValue) consulta = consulta.Where(u => u.Bloqueado == bloqueado.Value);
        if (!string.IsNullOrWhiteSpace(rol))
            consulta = consulta.Where(u => dbContext.UserRoles.Join(dbContext.Roles,
                ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Any(x => x.UserId == u.Id && x.Name == rol.Trim()));

        return await consulta.OrderBy(u => u.NombreCompleto).ThenBy(u => u.Id)
            .Select(u => new UsuarioDetalleResponse(u.Id, u.Cedula, u.NombreCompleto, u.Email ?? string.Empty,
                dbContext.UserRoles.Where(ur => ur.UserId == u.Id).Join(dbContext.Roles,
                    ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!).OrderBy(n => n).FirstOrDefault() ?? string.Empty,
                u.Bloqueado, u.VeterinarioId)).ToListAsync();
    }

    public async Task<UsuarioDetalleResponse?> ObtenerAsync(string id)
    {
        return await dbContext.Users.AsNoTracking().Where(u => u.Id == id)
            .Select(u => new UsuarioDetalleResponse(u.Id, u.Cedula, u.NombreCompleto, u.Email ?? string.Empty,
                dbContext.UserRoles.Where(ur => ur.UserId == u.Id).Join(dbContext.Roles,
                    ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!).OrderBy(n => n).FirstOrDefault() ?? string.Empty,
                u.Bloqueado, u.VeterinarioId)).SingleOrDefaultAsync();
    }

    public async Task<EstadoOperacionUsuario> ActualizarAsync(string id, ActualizarUsuarioRequest request)
    {
        if (!RolValido(request.Rol) || (request.Rol == RolVeterinario) != request.VeterinarioId.HasValue)
            return EstadoOperacionUsuario.DatosInvalidos;
        if (request.VeterinarioId.HasValue && !await dbContext.Veterinarios.AnyAsync(v => v.Id == request.VeterinarioId))
            return EstadoOperacionUsuario.DatosInvalidos;
        if (request.Contrasena is not null && string.IsNullOrWhiteSpace(request.Contrasena))
            return EstadoOperacionUsuario.DatosInvalidos;

        try
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var user = await userManager.FindByIdAsync(id);
            if (user is null) return EstadoOperacionUsuario.NoEncontrado;
            var roles = await userManager.GetRolesAsync(user);
            var rolesChange = roles.Count != 1 || roles[0] != request.Rol;
            if (rolesChange && roles.Contains(RolAdministrador) && !user.Bloqueado &&
                await ContarAdministradoresActivosAsync() <= 1) return EstadoOperacionUsuario.Conflicto;
            if (rolesChange && roles.Contains(RolDueno) && request.Rol != RolDueno &&
                await dbContext.Mascotas.AnyAsync(m => m.DuenoId == id)) return EstadoOperacionUsuario.Conflicto;
            var normalizedEmail = userManager.NormalizeEmail(request.Correo);
            if (await dbContext.Users.AnyAsync(u => u.Id != id && (u.Cedula == request.Cedula || u.NormalizedEmail == normalizedEmail)))
                return EstadoOperacionUsuario.Conflicto;
            if (request.VeterinarioId.HasValue && await dbContext.Users.AnyAsync(u => u.Id != id && u.VeterinarioId == request.VeterinarioId))
                return EstadoOperacionUsuario.Conflicto;

            if (!string.Equals(user.Email, request.Correo, StringComparison.OrdinalIgnoreCase))
            {
                var emailResult = await userManager.SetEmailAsync(user, request.Correo);
                if (!emailResult.Succeeded) return EsConflicto(emailResult) ? EstadoOperacionUsuario.Conflicto : EstadoOperacionUsuario.DatosInvalidos;
                var nameResult = await userManager.SetUserNameAsync(user, request.Correo);
                if (!nameResult.Succeeded) return EsConflicto(nameResult) ? EstadoOperacionUsuario.Conflicto : EstadoOperacionUsuario.DatosInvalidos;
            }
            user.Cedula = request.Cedula;
            user.NombreCompleto = request.NombreCompleto;
            user.VeterinarioId = request.VeterinarioId;
            var update = await userManager.UpdateAsync(user);
            if (!update.Succeeded) return EsConflicto(update) ? EstadoOperacionUsuario.Conflicto : EstadoOperacionUsuario.DatosInvalidos;

            if (rolesChange)
            {
                if (roles.Count > 0)
                {
                    var removed = await userManager.RemoveFromRolesAsync(user, roles);
                    if (!removed.Succeeded) return EstadoOperacionUsuario.Error;
                }
                var added = await userManager.AddToRoleAsync(user, request.Rol);
                if (!added.Succeeded) return EstadoOperacionUsuario.Error;
            }
            if (request.Contrasena is not null)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var password = await userManager.ResetPasswordAsync(user, token, request.Contrasena);
                if (!password.Succeeded) return EstadoOperacionUsuario.DatosInvalidos;
            }
            await tx.CommitAsync();
            return EstadoOperacionUsuario.Completado;
        }
        catch (DbUpdateException)
        {
            return EstadoOperacionUsuario.Conflicto;
        }
    }

    public async Task<EstadoOperacionUsuario> CambiarBloqueoAsync(string id, bool bloqueado)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return EstadoOperacionUsuario.NoEncontrado;
        if (bloqueado && !user.Bloqueado && (await userManager.IsInRoleAsync(user, RolAdministrador)) &&
            await ContarAdministradoresActivosAsync() <= 1) return EstadoOperacionUsuario.Conflicto;
        user.Bloqueado = bloqueado;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) return EstadoOperacionUsuario.Error;
        await tx.CommitAsync();
        return EstadoOperacionUsuario.Completado;
    }

    public async Task<EstadoOperacionUsuario> EliminarAsync(string id)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return EstadoOperacionUsuario.NoEncontrado;
        if (await TieneReferenciasAsync(id)) return EstadoOperacionUsuario.Conflicto;
        if (!user.Bloqueado && await userManager.IsInRoleAsync(user, RolAdministrador) &&
            await ContarAdministradoresActivosAsync() <= 1) return EstadoOperacionUsuario.Conflicto;
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded) return EstadoOperacionUsuario.Error;
        await tx.CommitAsync();
        return EstadoOperacionUsuario.Completado;
    }

    private async Task<int> ContarAdministradoresActivosAsync() => await (
        from ur in dbContext.UserRoles
        join role in dbContext.Roles on ur.RoleId equals role.Id
        join user in dbContext.Users on ur.UserId equals user.Id
        where role.Name == RolAdministrador && !user.Bloqueado
        select user.Id).Distinct().CountAsync();

    private async Task<bool> TieneReferenciasAsync(string id) =>
        await dbContext.Mascotas.AnyAsync(x => x.DuenoId == id) ||
        await dbContext.VacunasAplicadas.AnyAsync(x => x.RegistradoPorId == id) ||
        await dbContext.AsignacionesClinicas.AnyAsync(x => x.RegistradoPorId == id || x.SuspendidoPorId == id) ||
        await dbContext.Examenes.AnyAsync(x => x.RegistradoPorId == id) ||
        await dbContext.NotasClinicas.AnyAsync(x => x.RegistradoPorId == id);

    private static bool RolValido(string rol) => rol is RolAdministrador or RolVeterinario or RolDueno;

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

public enum EstadoOperacionUsuario { Completado, NoEncontrado, DatosInvalidos, Conflicto, Error }
