using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs.Auth;
using Veterinaria.Api.Models;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<Usuario> userManager,
    TokenService tokenService,
    DuenosService duenosService) : ControllerBase
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
        var resultado = await duenosService.CrearAsync(request);
        if (resultado.Estado == EstadoCreacionDueno.Conflicto)
        {
            return Conflict(new { mensaje = "El correo o la cédula ya están registrados." });
        }

        if (resultado.Estado == EstadoCreacionDueno.DatosInvalidos)
        {
            return BadRequest(new { errores = resultado.Errores });
        }

        if (resultado.Estado == EstadoCreacionDueno.Error)
        {
            return ErrorAsignacionRol();
        }

        return StatusCode(StatusCodes.Status201Created,
            new { mensaje = "Cuenta creada. Iniciá sesión para obtener un token." });
    }

    private ObjectResult ErrorAsignacionRol()
    {
        return Problem(
            title: "No se pudo completar el registro.",
            detail: "No fue posible asignar el rol de dueño. Inténtelo más tarde.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
}
