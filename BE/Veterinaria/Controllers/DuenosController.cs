using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs.Auth;
using Veterinaria.Api.DTOs.Duenos;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DuenosController(DuenosService duenosService) : ControllerBase
{
    [Authorize(Roles = "Administrador,Veterinario")]
    [HttpPost]
    public async Task<IActionResult> Crear(RegistroDuenoRequest request)
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
            return Problem(
                title: "No se pudo completar el registro.",
                detail: "No fue posible asignar el rol de dueño. Inténtelo más tarde.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var usuario = resultado.Usuario!;
        return StatusCode(StatusCodes.Status201Created,
            new DuenoResponse(usuario.Id, usuario.Cedula, usuario.NombreCompleto));
    }

    [Authorize(Roles = "Administrador,Veterinario")]
    [HttpGet("por-cedula")]
    public async Task<ActionResult<DuenoResponse>> BuscarPorCedula([FromQuery] string? cedula)
    {
        if (string.IsNullOrWhiteSpace(cedula))
        {
            return BadRequest(new { mensaje = "La cédula es obligatoria." });
        }

        var dueno = await duenosService.BuscarPorCedulaAsync(cedula);
        if (dueno is null)
        {
            return NotFound();
        }

        return Ok(dueno);
    }
}
