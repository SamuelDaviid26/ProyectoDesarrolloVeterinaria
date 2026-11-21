using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs.Razas;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RazasController(RazasService razasService) : ControllerBase
{
    [Authorize(Roles = "Administrador,Veterinario")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RazaResponse>>> Listar(
        [FromQuery] int? especieId,
        [FromQuery] string? nombre)
    {
        return Ok(await razasService.ListarAsync(especieId, nombre));
    }

    [Authorize(Roles = "Administrador,Veterinario")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RazaResponse>> Obtener(int id)
    {
        var raza = await razasService.ObtenerAsync(id);
        return raza is null ? NotFound() : Ok(raza);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Crear(RazaRequest request)
    {
        var resultado = await razasService.CrearAsync(request);
        if (resultado.Estado == EstadoEscrituraRaza.EspecieNoExiste)
        {
            return BadRequest(new { mensaje = "La especie indicada no existe." });
        }

        var raza = resultado.Raza!;
        return CreatedAtAction(nameof(Obtener), new { id = raza.Id }, raza);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, RazaRequest request)
    {
        var resultado = await razasService.ActualizarAsync(id, request);
        return resultado switch
        {
            EstadoEscrituraRaza.Completada => NoContent(),
            EstadoEscrituraRaza.RazaNoExiste => NotFound(),
            EstadoEscrituraRaza.EspecieNoExiste => BadRequest(new { mensaje = "La especie indicada no existe." }),
            EstadoEscrituraRaza.TieneMascotas => Conflict(new
            {
                mensaje = "No se puede cambiar la especie porque la raza tiene mascotas asociadas."
            }),
            _ => throw new InvalidOperationException("Resultado de actualización no reconocido.")
        };
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await razasService.EliminarAsync(id);
        return resultado switch
        {
            EstadoEscrituraRaza.Completada => NoContent(),
            EstadoEscrituraRaza.RazaNoExiste => NotFound(),
            EstadoEscrituraRaza.TieneMascotas => Conflict(new
            {
                mensaje = "No se puede eliminar la raza porque tiene mascotas asociadas."
            }),
            _ => throw new InvalidOperationException("Resultado de eliminación no reconocido.")
        };
    }
}
