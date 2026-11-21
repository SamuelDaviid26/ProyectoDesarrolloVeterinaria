using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Data;

public class InicializadorIdentity(
    RoleManager<IdentityRole> roleManager,
    UserManager<Usuario> userManager,
    VeterinariaDbContext dbContext,
    IConfiguration configuration)
{
    private static readonly string[] Roles = ["Administrador", "Veterinario", "Dueño"];

    public async Task InicializarAsync()
    {
        foreach (var nombreRol in Roles)
        {
            if (await roleManager.RoleExistsAsync(nombreRol))
            {
                continue;
            }

            var resultadoRol = await roleManager.CreateAsync(new IdentityRole(nombreRol));
            AsegurarExito(resultadoRol, $"No se pudo crear el rol '{nombreRol}'");
        }

        if ((await userManager.GetUsersInRoleAsync("Administrador")).Count > 0)
        {
            return;
        }

        var correo = ObtenerRequerido("AdminInicial:Correo");
        var password = ObtenerRequerido("AdminInicial:Password");
        var cedula = ObtenerRequerido("AdminInicial:Cedula");
        var nombreCompleto = ObtenerRequerido("AdminInicial:NombreCompleto");

        if (await userManager.FindByEmailAsync(correo) is not null)
        {
            throw new InvalidOperationException(
                $"No se pudo crear el administrador inicial: el correo '{correo}' ya pertenece a una cuenta. No se modificó esa cuenta.");
        }

        if (await dbContext.Users.AnyAsync(usuario => usuario.Cedula == cedula))
        {
            throw new InvalidOperationException(
                "No se pudo crear el administrador inicial: la cédula configurada ya pertenece a una cuenta. No se modificó esa cuenta.");
        }

        await using var transaccion = await dbContext.Database.BeginTransactionAsync();

        var administrador = new Usuario
        {
            Email = correo,
            UserName = correo,
            Cedula = cedula,
            NombreCompleto = nombreCompleto,
            Bloqueado = false,
            VeterinarioId = null
        };

        var resultadoCreacion = await userManager.CreateAsync(administrador, password);
        AsegurarExito(resultadoCreacion, "No se pudo crear el administrador inicial");

        var resultadoRolAdmin = await userManager.AddToRoleAsync(administrador, "Administrador");
        AsegurarExito(resultadoRolAdmin, "No se pudo asignar el rol Administrador al usuario inicial");

        await transaccion.CommitAsync();
    }

    private string ObtenerRequerido(string clave)
    {
        var valor = configuration[clave];
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new InvalidOperationException(
                $"Falta configurar '{clave}' para crear el administrador inicial. Configure las credenciales con User Secrets.");
        }

        return valor;
    }

    private static void AsegurarExito(IdentityResult resultado, string mensaje)
    {
        if (!resultado.Succeeded)
        {
            var errores = string.Join("; ", resultado.Errors.Select(error => $"{error.Code}: {error.Description}"));
            throw new InvalidOperationException($"{mensaje}: {errores}");
        }
    }
}
