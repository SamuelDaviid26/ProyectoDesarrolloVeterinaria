using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/mis-mascotas")]
[Authorize(Roles = "Dueño")]
public class MisMascotasController(
    MascotasService mascotasService,
    ExpedientesService expedientesService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar()
        => Ok(await mascotasService.ListarDelDuenoAsync(DuenoId()));

    [HttpGet("{id:int}/foto")]
    public async Task<IActionResult> ObtenerFoto(int id)
    {
        var foto = await mascotasService.ObtenerFotoAsync(id, DuenoId());
        return foto is null ? NotFound() : File(foto.Contenido, foto.TipoContenido);
    }

    [HttpGet("{id:int}/vacunas")]
    public async Task<IActionResult> ListarVacunas(int id)
    {
        var vacunas = await expedientesService.ListarVacunasAsync(id, DuenoId());
        return vacunas is null ? NotFound() : Ok(vacunas);
    }

    [HttpGet("{id:int}/tratamientos")]
    public async Task<IActionResult> ListarTratamientos(int id)
    {
        var tratamientos = await expedientesService.ListarTratamientosAsync(id, DuenoId());
        return tratamientos is null ? NotFound() : Ok(tratamientos);
    }

    [HttpGet("{id:int}/examenes")]
    public async Task<IActionResult> ListarExamenes(int id)
    {
        var examenes = await expedientesService.ListarExamenesAsync(id, DuenoId());
        return examenes is null ? NotFound() : Ok(examenes);
    }

    [HttpGet("{id:int}/examenes/{examenId:int}/archivo")]
    public async Task<IActionResult> ObtenerArchivoExamen(int id, int examenId)
    {
        var archivo = await expedientesService.ObtenerArchivoExamenAsync(id, examenId, DuenoId());
        return archivo is null ? NotFound() : File(archivo.Contenido, archivo.TipoContenido, archivo.Nombre);
    }

    [HttpGet("{id:int}/notas")]
    public async Task<IActionResult> ListarNotas(int id)
    {
        var notas = await expedientesService.ListarNotasAsync(id, DuenoId());
        return notas is null ? NotFound() : Ok(notas);
    }

    private string DuenoId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}
