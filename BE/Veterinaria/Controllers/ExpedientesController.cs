using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs.Expedientes;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/mascotas/{mascotaId:int}")]
[Authorize(Roles = "Administrador,Veterinario")]
public class ExpedientesController(ExpedientesService expedientesService) : ControllerBase
{
    [HttpGet("vacunas")]
    public async Task<IActionResult> ListarVacunas(int mascotaId)
    {
        var result = await expedientesService.ListarVacunasAsync(mascotaId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("vacunas")]
    public async Task<IActionResult> CrearVacuna(int mascotaId, CrearVacunaRequest request)
    {
        var result = await expedientesService.CrearVacunaAsync(mascotaId, ActorId(), request);
        if (result.Estado != EstadoExpediente.Completado) return Error(result.Estado, result.Error);
        var vacuna = result.Valor!;
        return Created($"/api/mascotas/{mascotaId}/vacunas", vacuna);
    }

    [HttpGet("tratamientos")]
    public async Task<IActionResult> ListarTratamientos(int mascotaId)
    {
        var result = await expedientesService.ListarTratamientosAsync(mascotaId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("tratamientos")]
    public async Task<IActionResult> CrearTratamiento(int mascotaId, CrearTratamientoRequest request)
    {
        var result = await expedientesService.CrearTratamientoAsync(mascotaId, ActorId(), request);
        if (result.Estado != EstadoExpediente.Completado) return Error(result.Estado, result.Error);
        var item = result.Valor!;
        return Created($"/api/mascotas/{mascotaId}/tratamientos", item);
    }

    [HttpPatch("tratamientos/{id:int}/suspension")]
    public async Task<IActionResult> SuspenderTratamiento(int mascotaId, int id, SuspenderTratamientoRequest request)
    {
        var result = await expedientesService.SuspenderTratamientoAsync(mascotaId, id, ActorId(), request);
        return result.Estado == EstadoExpediente.Completado ? NoContent() : Error(result.Estado, result.Error);
    }

    [HttpGet("examenes")]
    public async Task<IActionResult> ListarExamenes(int mascotaId)
    {
        var result = await expedientesService.ListarExamenesAsync(mascotaId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("examenes")]
    public async Task<IActionResult> CrearExamen(int mascotaId, [FromForm] CrearExamenRequest request)
    {
        var result = await expedientesService.CrearExamenAsync(mascotaId, ActorId(), request);
        if (result.Estado != EstadoExpediente.Completado) return Error(result.Estado, result.Error);
        var examen = result.Valor!;
        return Created($"/api/mascotas/{mascotaId}/examenes/{examen.Id}/archivo", examen);
    }

    [HttpGet("examenes/{id:int}/archivo")]
    public async Task<IActionResult> ObtenerArchivoExamen(int mascotaId, int id)
    {
        var archivo = await expedientesService.ObtenerArchivoExamenAsync(mascotaId, id);
        return archivo is null ? NotFound() : File(archivo.Contenido, archivo.TipoContenido, archivo.Nombre);
    }

    [HttpGet("notas")]
    public async Task<IActionResult> ListarNotas(int mascotaId)
    {
        var result = await expedientesService.ListarNotasAsync(mascotaId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("notas")]
    public async Task<IActionResult> CrearNota(int mascotaId, CrearNotaRequest request)
    {
        var result = await expedientesService.CrearNotaAsync(mascotaId, ActorId(), request);
        if (result.Estado != EstadoExpediente.Completado) return Error(result.Estado, result.Error);
        var nota = result.Valor!;
        return Created($"/api/mascotas/{mascotaId}/notas", nota);
    }

    private string ActorId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private IActionResult Error(EstadoExpediente estado, string? mensaje) => estado switch
    {
        EstadoExpediente.NoExiste => NotFound(),
        EstadoExpediente.DatosInvalidos => BadRequest(new { mensaje }),
        EstadoExpediente.Conflicto => Conflict(new { mensaje }),
        _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
    };
}
