using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs.CatalogoClinico;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/catalogo-clinico")]
public class CatalogoClinicoController(CatalogoClinicoService catalogoClinicoService) : ControllerBase
{
    [Authorize(Roles = "Administrador,Veterinario")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CatalogoClinicoResponse>>> Listar(
        [FromQuery] string? nombre,
        [FromQuery] string? tipo)
    {
        if (tipo is not null && !CatalogoClinicoService.EsTipoValido(tipo))
        {
            return BadRequest(new { mensaje = "El tipo debe ser Medicamento o Tratamiento." });
        }

        return Ok(await catalogoClinicoService.ListarAsync(nombre, tipo));
    }

    [Authorize(Roles = "Administrador,Veterinario")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CatalogoClinicoResponse>> Obtener(int id)
    {
        var elemento = await catalogoClinicoService.ObtenerAsync(id);
        return elemento is null ? NotFound() : Ok(elemento);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<ActionResult<CatalogoClinicoResponse>> Crear(CatalogoClinicoRequest request)
    {
        var elemento = await catalogoClinicoService.CrearAsync(request);
        return CreatedAtAction(nameof(Obtener), new { id = elemento.Id }, elemento);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, CatalogoClinicoRequest request)
    {
        return await catalogoClinicoService.ActualizarAsync(id, request) ? NoContent() : NotFound();
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await catalogoClinicoService.EliminarAsync(id);
        return resultado switch
        {
            EstadoEliminacionCatalogoClinico.Eliminado => NoContent(),
            EstadoEliminacionCatalogoClinico.NoExiste => NotFound(),
            EstadoEliminacionCatalogoClinico.TieneAsignaciones => Conflict(new
            {
                mensaje = "No se puede eliminar el elemento porque tiene asignaciones en el expediente clínico."
            }),
            _ => throw new InvalidOperationException("Resultado de eliminación no reconocido.")
        };
    }
}
