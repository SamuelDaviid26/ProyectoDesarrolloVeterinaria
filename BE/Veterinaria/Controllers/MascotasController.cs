using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs.Mascotas;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,Veterinario")]
public class MascotasController(MascotasService mascotasService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MascotasListadoResponse>> Listar(
        [FromQuery] string? nombre, [FromQuery] string? cedulaDueno)
        => Ok(await mascotasService.ListarAsync(nombre, cedulaDueno));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MascotaResponse>> Obtener(int id)
    {
        var mascota = await mascotasService.ObtenerAsync(id);
        return mascota is null ? NotFound() : Ok(mascota);
    }

    [HttpGet("{id:int}/foto")]
    public async Task<IActionResult> ObtenerFoto(int id)
    {
        var foto = await mascotasService.ObtenerFotoAsync(id);
        return foto is null ? NotFound() : File(foto.Contenido, foto.TipoContenido);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromForm] CrearMascotaRequest request)
    {
        var resultado = await mascotasService.CrearAsync(request);
        if (resultado.Estado == EstadoMascota.DatosInvalidos)
            return BadRequest(new { mensaje = resultado.Error });
        var mascota = resultado.Mascota!;
        return CreatedAtAction(nameof(Obtener), new { id = mascota.Id }, mascota);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromForm] ActualizarMascotaRequest request)
    {
        var resultado = await mascotasService.ActualizarAsync(id, request);
        return resultado switch
        {
            EstadoMascota.Completado => NoContent(),
            EstadoMascota.NoExiste => NotFound(),
            _ => BadRequest(new { mensaje = "Los datos de la mascota, raza, dueño o foto no son válidos." })
        };
    }
}
