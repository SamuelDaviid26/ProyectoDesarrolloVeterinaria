using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs.Auth;
using Veterinaria.Api.Models;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(UserManager<Usuario> userManager, TokenService tokenService) : ControllerBase
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
}
