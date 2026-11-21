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
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? nombre, [FromQuery] string? cedula,
        [FromQuery] string? correo, [FromQuery] string? rol, [FromQuery] bool? bloqueado)
        => Ok(await usuariosService.ListarAsync(nombre, cedula, correo, rol, bloqueado));

    [HttpGet("{id}")]
    public async Task<IActionResult> Obtener(string id)
    {
        var usuario = await usuariosService.ObtenerAsync(id);
        return usuario is null ? NotFound() : Ok(usuario);
    }

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

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(string id, ActualizarUsuarioRequest request)
        => ResultadoOperacion(await usuariosService.ActualizarAsync(id, request));

    [HttpPatch("{id}/bloqueo")]
    public async Task<IActionResult> CambiarBloqueo(string id, CambiarBloqueoRequest request)
    {
        if (!request.Bloqueado.HasValue) return BadRequest(new { mensaje = "El campo bloqueado es obligatorio." });
        return ResultadoOperacion(await usuariosService.CambiarBloqueoAsync(id, request.Bloqueado.Value));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(string id)
        => ResultadoOperacion(await usuariosService.EliminarAsync(id));

    private IActionResult ResultadoOperacion(EstadoOperacionUsuario estado) => estado switch
    {
        EstadoOperacionUsuario.Completado => NoContent(),
        EstadoOperacionUsuario.NoEncontrado => NotFound(),
        EstadoOperacionUsuario.DatosInvalidos => BadRequest(new { mensaje = "Los datos del usuario no son válidos." }),
        EstadoOperacionUsuario.Conflicto => Conflict(new { mensaje = "La operación entra en conflicto con una cuenta o registro existente." }),
        _ => Problem(title: "No se pudo completar la operación.", statusCode: StatusCodes.Status500InternalServerError)
    };
}
