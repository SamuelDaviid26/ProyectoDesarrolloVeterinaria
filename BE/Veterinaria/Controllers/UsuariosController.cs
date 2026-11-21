using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs.Usuarios;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class UsuariosController(UsuariosService usuariosService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Crear(CrearUsuarioRequest request)
    {
        var resultado = await usuariosService.CrearAsync(request);
        if (resultado.Estado == EstadoCreacionUsuario.Conflicto)
        {
            return Conflict(new { mensaje = "El correo, la cédula o la ficha profesional ya están vinculados." });
        }

        if (resultado.Estado == EstadoCreacionUsuario.DatosInvalidos)
        {
            return BadRequest(new { errores = resultado.Errores });
        }

        if (resultado.Estado == EstadoCreacionUsuario.Error)
        {
            return Problem(
                title: "No se pudo completar el alta del usuario.",
                detail: "No fue posible asignar el rol. Inténtelo más tarde.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return StatusCode(StatusCodes.Status201Created,
            UsuariosService.ARespuesta(resultado.Usuario!, request.Rol));
    }
}
