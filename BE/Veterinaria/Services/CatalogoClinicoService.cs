using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.DTOs.CatalogoClinico;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Services;

public enum EstadoEliminacionCatalogoClinico
{
    Eliminado,
    NoExiste,
    TieneAsignaciones
}

public class CatalogoClinicoService(VeterinariaDbContext dbContext)
{
    public async Task<IReadOnlyList<CatalogoClinicoResponse>> ListarAsync(string? nombre, string? tipo)
    {
        var consulta = dbContext.ElementosCatalogoClinico.AsNoTracking().AsQueryable();
        var filtroNombre = nombre?.Trim();
        if (!string.IsNullOrEmpty(filtroNombre))
        {
            consulta = consulta.Where(elemento => elemento.Nombre.Contains(filtroNombre));
        }

        if (tipo is not null)
        {
            var tipoEnum = ParseTipo(tipo);
            consulta = consulta.Where(elemento => elemento.Tipo == tipoEnum);
        }

        return await consulta
            .OrderBy(elemento => elemento.Nombre)
            .ThenBy(elemento => elemento.Id)
            .Select(elemento => new CatalogoClinicoResponse(
                elemento.Id,
                elemento.Nombre,
                elemento.Tipo.ToString(),
                elemento.Descripcion))
            .ToListAsync();
    }

    public Task<CatalogoClinicoResponse?> ObtenerAsync(int id)
    {
        return dbContext.ElementosCatalogoClinico
            .AsNoTracking()
            .Where(elemento => elemento.Id == id)
            .Select(elemento => new CatalogoClinicoResponse(
                elemento.Id,
                elemento.Nombre,
                elemento.Tipo.ToString(),
                elemento.Descripcion))
            .SingleOrDefaultAsync();
    }

    public async Task<CatalogoClinicoResponse> CrearAsync(CatalogoClinicoRequest request)
    {
        var elemento = new ElementoCatalogoClinico
        {
            Nombre = request.Nombre.Trim(),
            Tipo = ParseTipo(request.Tipo),
            Descripcion = request.Descripcion.Trim()
        };

        dbContext.ElementosCatalogoClinico.Add(elemento);
        await dbContext.SaveChangesAsync();
        return ARespuesta(elemento);
    }

    public async Task<bool> ActualizarAsync(int id, CatalogoClinicoRequest request)
    {
        var elemento = await dbContext.ElementosCatalogoClinico.FindAsync(id);
        if (elemento is null)
        {
            return false;
        }

        elemento.Nombre = request.Nombre.Trim();
        elemento.Tipo = ParseTipo(request.Tipo);
        elemento.Descripcion = request.Descripcion.Trim();
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<EstadoEliminacionCatalogoClinico> EliminarAsync(int id)
    {
        var elemento = await dbContext.ElementosCatalogoClinico.FindAsync(id);
        if (elemento is null)
        {
            return EstadoEliminacionCatalogoClinico.NoExiste;
        }

        if (await dbContext.AsignacionesClinicas.AnyAsync(asignacion =>
                asignacion.ElementoCatalogoClinicoId == id))
        {
            return EstadoEliminacionCatalogoClinico.TieneAsignaciones;
        }

        dbContext.ElementosCatalogoClinico.Remove(elemento);
        try
        {
            await dbContext.SaveChangesAsync();
            return EstadoEliminacionCatalogoClinico.Eliminado;
        }
        catch (DbUpdateException)
        {
            if (await dbContext.AsignacionesClinicas.AnyAsync(asignacion =>
                    asignacion.ElementoCatalogoClinicoId == id))
            {
                return EstadoEliminacionCatalogoClinico.TieneAsignaciones;
            }

            throw;
        }
    }

    public static bool EsTipoValido(string tipo)
    {
        return tipo is nameof(TipoElementoClinico.Medicamento) or nameof(TipoElementoClinico.Tratamiento);
    }

    private static TipoElementoClinico ParseTipo(string tipo)
    {
        return tipo switch
        {
            nameof(TipoElementoClinico.Medicamento) => TipoElementoClinico.Medicamento,
            nameof(TipoElementoClinico.Tratamiento) => TipoElementoClinico.Tratamiento,
            _ => throw new ArgumentException("El tipo debe ser Medicamento o Tratamiento.", nameof(tipo))
        };
    }

    private static CatalogoClinicoResponse ARespuesta(ElementoCatalogoClinico elemento)
    {
        return new CatalogoClinicoResponse(
            elemento.Id,
            elemento.Nombre,
            elemento.Tipo.ToString(),
            elemento.Descripcion);
    }
}
