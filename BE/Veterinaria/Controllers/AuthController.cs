using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.DTOs.Auth;
using Veterinaria.Api.Models;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<Usuario> userManager,
    TokenService tokenService,
    VeterinariaDbContext dbContext) : ControllerBase
{
    private const string MensajeCredencialesInvalidas = "Correo o contraseña incorrectos, o cuenta bloqueada.";

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var usuario = await userManager.FindByEmailAsync(request.Correo);
        if (usuario is null || usuario.Bloqueado ||
            !await userManager.CheckPasswordAsync(usuario, request.Contrasena))
        {
            return Unauthorized(new { mensaje = MensajeCredencialesInvalidas });
        }

        var roles = await userManager.GetRolesAsync(usuario);
        var rol = roles.FirstOrDefault();
        if (rol is null)
        {
            return Unauthorized(new { mensaje = MensajeCredencialesInvalidas });
        }

        return Ok(tokenService.CrearToken(usuario, rol));
    }

    [AllowAnonymous]
    [HttpPost("registro")]
    public async Task<IActionResult> Registro(RegistroDuenoRequest request)
    {
        if (await userManager.FindByEmailAsync(request.Correo) is not null ||
            await dbContext.Users.AnyAsync(usuario => usuario.Cedula == request.Cedula))
        {
            return Conflict(new { mensaje = "El correo o la cédula ya están registrados." });
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
                    return Conflict(new { mensaje = "El correo o la cédula ya están registrados." });
                }

                return BadRequest(new
                {
                    errores = resultadoCreacion.Errors.Select(error => error.Description)
                });
            }

            var resultadoRol = await userManager.AddToRoleAsync(usuario, "Dueño");
            if (!resultadoRol.Succeeded)
            {
                await transaccion.RollbackAsync();
                return Problem(
                    title: "No se pudo completar el registro.",
                    detail: "No fue posible asignar el rol de dueño. Inténtelo más tarde.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            await transaccion.CommitAsync();
        }
        catch (DbUpdateException)
        {
            if (await userManager.FindByEmailAsync(request.Correo) is not null ||
                await dbContext.Users.AnyAsync(otroUsuario => otroUsuario.Cedula == request.Cedula))
            {
                return Conflict(new { mensaje = "El correo o la cédula ya están registrados." });
            }

            throw;
        }

        return StatusCode(StatusCodes.Status201Created,
            new { mensaje = "Cuenta creada. Iniciá sesión para obtener un token." });
    }

    private static bool EsConflicto(IdentityResult resultado)
    {
        return resultado.Errors.Any(error =>
            error.Code is "DuplicateEmail" or "DuplicateUserName");
    }
}
