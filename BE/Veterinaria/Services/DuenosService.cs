using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.DTOs.Auth;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Services;

public enum EstadoCreacionDueno
{
    Creado,
    Conflicto,
    DatosInvalidos,
    Error
}

public sealed record ResultadoCreacionDueno(
    EstadoCreacionDueno Estado,
    Usuario? Usuario = null,
    IReadOnlyList<string>? Errores = null);

public class DuenosService(UserManager<Usuario> userManager, VeterinariaDbContext dbContext)
{
    private const string RolDueno = "Dueño";

    public async Task<ResultadoCreacionDueno> CrearAsync(RegistroDuenoRequest request)
    {
        if (await userManager.FindByEmailAsync(request.Correo) is not null ||
            await dbContext.Users.AnyAsync(usuario => usuario.Cedula == request.Cedula))
        {
            return new ResultadoCreacionDueno(EstadoCreacionDueno.Conflicto);
        }

        var usuario = new Usuario
        {
            Cedula = request.Cedula,
            NombreCompleto = request.NombreCompleto,
            Email = request.Correo,
            UserName = request.Correo,
            Bloqueado = false,
            VeterinarioId = null
        };

        try
        {
            await using var transaccion = await dbContext.Database.BeginTransactionAsync();

            var resultadoCreacion = await userManager.CreateAsync(usuario, request.Contrasena);
            if (!resultadoCreacion.Succeeded)
            {
                await transaccion.RollbackAsync();
                if (EsConflicto(resultadoCreacion))
                {
                    return new ResultadoCreacionDueno(EstadoCreacionDueno.Conflicto);
                }

                return new ResultadoCreacionDueno(
                    EstadoCreacionDueno.DatosInvalidos,
                    Errores: resultadoCreacion.Errors.Select(error => error.Description).ToArray());
            }

            var resultadoRol = await userManager.AddToRoleAsync(usuario, RolDueno);
            if (!resultadoRol.Succeeded)
            {
                await transaccion.RollbackAsync();
                return new ResultadoCreacionDueno(
                    EstadoCreacionDueno.Error,
                    Errores: resultadoRol.Errors.Select(error => error.Description).ToArray());
            }

            await transaccion.CommitAsync();
            return new ResultadoCreacionDueno(EstadoCreacionDueno.Creado, usuario);
        }
        catch (DbUpdateException)
        {
            if (await userManager.FindByEmailAsync(request.Correo) is not null ||
                await dbContext.Users.AnyAsync(otroUsuario => otroUsuario.Cedula == request.Cedula))
            {
                return new ResultadoCreacionDueno(EstadoCreacionDueno.Conflicto);
            }

            throw;
        }
    }

    private static bool EsConflicto(IdentityResult resultado)
    {
        return resultado.Errors.Any(error =>
            error.Code is "DuplicateEmail" or "DuplicateUserName");
    }
}
