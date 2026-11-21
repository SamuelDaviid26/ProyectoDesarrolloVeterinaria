using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs.Veterinarios;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class VeterinariosController(VeterinariosService veterinariosService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VeterinarioResponse>>> Listar(
        [FromQuery] string? nombre,
        [FromQuery] string? numeroColegiado)
    {
        return Ok(await veterinariosService.ListarAsync(nombre, numeroColegiado));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VeterinarioResponse>> Obtener(int id)
    {
        var veterinario = await veterinariosService.ObtenerAsync(id);
        return veterinario is null ? NotFound() : Ok(veterinario);
    }

    [HttpGet("{id:int}/foto")]
    public async Task<IActionResult> ObtenerFoto(int id)
    {
        var foto = await veterinariosService.ObtenerFotoAsync(id);
        return foto is null ? NotFound() : File(foto.Contenido, foto.TipoContenido);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromForm] CrearVeterinarioRequest request)
    {
        var resultado = await veterinariosService.CrearAsync(request);
        if (resultado.Estado == EstadoVeterinario.DatosInvalidos)
        {
            return BadRequest(new { mensaje = resultado.Error });
        }

        var veterinario = resultado.Veterinario!;
        return CreatedAtAction(nameof(Obtener), new { id = veterinario.Id }, veterinario);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromForm] ActualizarVeterinarioRequest request)
    {
        var resultado = await veterinariosService.ActualizarAsync(id, request);
        return resultado.Estado switch
        {
            EstadoVeterinario.Completado => NoContent(),
            EstadoVeterinario.NoExiste => NotFound(),
            EstadoVeterinario.DatosInvalidos => BadRequest(new { mensaje = resultado.Error }),
            _ => throw new InvalidOperationException("Resultado de actualización no reconocido.")
        };
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await veterinariosService.EliminarAsync(id);
        return resultado switch
        {
            EstadoVeterinario.Completado => NoContent(),
            EstadoVeterinario.NoExiste => NotFound(),
            EstadoVeterinario.Conflicto => Conflict(new
            {
                mensaje = "No se puede eliminar el veterinario porque está vinculado a una cuenta o al expediente clínico."
            }),
            _ => throw new InvalidOperationException("Resultado de eliminación no reconocido.")
        };
    }
}
