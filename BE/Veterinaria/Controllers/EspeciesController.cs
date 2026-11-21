using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs.Especies;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EspeciesController(EspeciesService especiesService) : ControllerBase
{
    [Authorize(Roles = "Administrador,Veterinario")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EspecieResponse>>> Listar([FromQuery] string? nombre)
    {
        return Ok(await especiesService.ListarAsync(nombre));
    }

    [Authorize(Roles = "Administrador,Veterinario")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EspecieResponse>> Obtener(int id)
    {
        var especie = await especiesService.ObtenerAsync(id);
        return especie is null ? NotFound() : Ok(especie);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<ActionResult<EspecieResponse>> Crear(EspecieRequest request)
    {
        var especie = await especiesService.CrearAsync(request);
        return CreatedAtAction(nameof(Obtener), new { id = especie.Id }, especie);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, EspecieRequest request)
    {
        return await especiesService.ActualizarAsync(id, request) ? NoContent() : NotFound();
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await especiesService.EliminarAsync(id);
        return resultado switch
        {
            EstadoEliminacionEspecie.Eliminada => NoContent(),
            EstadoEliminacionEspecie.NoExiste => NotFound(),
            EstadoEliminacionEspecie.TieneRazas => Conflict(new
            {
                mensaje = "No se puede eliminar la especie porque tiene razas asociadas."
            }),
            _ => throw new InvalidOperationException("Resultado de eliminación no reconocido.")
        };
    }
}
