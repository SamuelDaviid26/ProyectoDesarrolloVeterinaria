using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.DTOs.Especies;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Services;

public enum EstadoEliminacionEspecie
{
    Eliminada,
    NoExiste,
    TieneRazas
}

public class EspeciesService(VeterinariaDbContext dbContext)
{
    public async Task<IReadOnlyList<EspecieResponse>> ListarAsync(string? nombre)
    {
        var consulta = dbContext.Especies.AsNoTracking().AsQueryable();
        var filtro = nombre?.Trim();
        if (!string.IsNullOrEmpty(filtro))
        {
            consulta = consulta.Where(especie => especie.Nombre.Contains(filtro));
        }

        return await consulta
            .OrderBy(especie => especie.Nombre)
            .ThenBy(especie => especie.Id)
            .Select(especie => new EspecieResponse(especie.Id, especie.Nombre))
            .ToListAsync();
    }

    public Task<EspecieResponse?> ObtenerAsync(int id)
    {
        return dbContext.Especies
            .AsNoTracking()
            .Where(especie => especie.Id == id)
            .Select(especie => new EspecieResponse(especie.Id, especie.Nombre))
            .SingleOrDefaultAsync();
    }

    public async Task<EspecieResponse> CrearAsync(EspecieRequest request)
    {
        var especie = new Especie { Nombre = request.Nombre.Trim() };
        dbContext.Especies.Add(especie);
        await dbContext.SaveChangesAsync();
        return new EspecieResponse(especie.Id, especie.Nombre);
    }

    public async Task<bool> ActualizarAsync(int id, EspecieRequest request)
    {
        var especie = await dbContext.Especies.FindAsync(id);
        if (especie is null)
        {
            return false;
        }

        especie.Nombre = request.Nombre.Trim();
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<EstadoEliminacionEspecie> EliminarAsync(int id)
    {
        var especie = await dbContext.Especies.FindAsync(id);
        if (especie is null)
        {
            return EstadoEliminacionEspecie.NoExiste;
        }

        if (await dbContext.Razas.AnyAsync(raza => raza.EspecieId == id))
        {
            return EstadoEliminacionEspecie.TieneRazas;
        }

        dbContext.Especies.Remove(especie);
        try
        {
            await dbContext.SaveChangesAsync();
            return EstadoEliminacionEspecie.Eliminada;
        }
        catch (DbUpdateException)
        {
            if (await dbContext.Razas.AnyAsync(raza => raza.EspecieId == id))
            {
                return EstadoEliminacionEspecie.TieneRazas;
            }

            throw;
        }
    }
}
