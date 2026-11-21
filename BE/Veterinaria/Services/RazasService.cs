using System.Data;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.DTOs.Razas;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Services;

public enum EstadoEscrituraRaza
{
    Completada,
    RazaNoExiste,
    EspecieNoExiste,
    TieneMascotas
}

public sealed record ResultadoEscrituraRaza(EstadoEscrituraRaza Estado, RazaResponse? Raza = null);

public class RazasService(VeterinariaDbContext dbContext)
{
    public async Task<IReadOnlyList<RazaResponse>> ListarAsync(int? especieId, string? nombre)
    {
        var consulta = dbContext.Razas.AsNoTracking().AsQueryable();
        var filtro = nombre?.Trim();
        if (especieId.HasValue)
        {
            consulta = consulta.Where(raza => raza.EspecieId == especieId.Value);
        }

        if (!string.IsNullOrEmpty(filtro))
        {
            consulta = consulta.Where(raza => raza.Nombre.Contains(filtro));
        }

        return await consulta
            .OrderBy(raza => raza.Nombre)
            .ThenBy(raza => raza.Id)
            .Select(raza => new RazaResponse(
                raza.Id,
                raza.Nombre,
                raza.EspecieId,
                raza.Especie!.Nombre))
            .ToListAsync();
    }

    public Task<RazaResponse?> ObtenerAsync(int id)
    {
        return dbContext.Razas
            .AsNoTracking()
            .Where(raza => raza.Id == id)
            .Select(raza => new RazaResponse(
                raza.Id,
                raza.Nombre,
                raza.EspecieId,
                raza.Especie!.Nombre))
            .SingleOrDefaultAsync();
    }

    public async Task<ResultadoEscrituraRaza> CrearAsync(RazaRequest request)
    {
        var especieNombre = await dbContext.Especies
            .Where(especie => especie.Id == request.EspecieId)
            .Select(especie => especie.Nombre)
            .SingleOrDefaultAsync();
        if (especieNombre is null)
        {
            return new ResultadoEscrituraRaza(EstadoEscrituraRaza.EspecieNoExiste);
        }

        var raza = new Raza
        {
            Nombre = request.Nombre.Trim(),
            EspecieId = request.EspecieId
        };

        try
        {
            dbContext.Razas.Add(raza);
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (!await dbContext.Especies.AnyAsync(especie => especie.Id == request.EspecieId))
            {
                return new ResultadoEscrituraRaza(EstadoEscrituraRaza.EspecieNoExiste);
            }

            throw;
        }

        return new ResultadoEscrituraRaza(
            EstadoEscrituraRaza.Completada,
            new RazaResponse(raza.Id, raza.Nombre, raza.EspecieId, especieNombre));
    }

    public async Task<EstadoEscrituraRaza> ActualizarAsync(int id, RazaRequest request)
    {
        await using var transaccion = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var raza = await dbContext.Razas.SingleOrDefaultAsync(elemento => elemento.Id == id);
        if (raza is null)
        {
            return EstadoEscrituraRaza.RazaNoExiste;
        }

        if (!await dbContext.Especies.AnyAsync(especie => especie.Id == request.EspecieId))
        {
            return EstadoEscrituraRaza.EspecieNoExiste;
        }

        if (raza.EspecieId != request.EspecieId &&
            await dbContext.Mascotas.AnyAsync(mascota => mascota.RazaId == id))
        {
            return EstadoEscrituraRaza.TieneMascotas;
        }

        raza.Nombre = request.Nombre.Trim();
        raza.EspecieId = request.EspecieId;
        await dbContext.SaveChangesAsync();
        await transaccion.CommitAsync();
        return EstadoEscrituraRaza.Completada;
    }

    public async Task<EstadoEscrituraRaza> EliminarAsync(int id)
    {
        var raza = await dbContext.Razas.FindAsync(id);
        if (raza is null)
        {
            return EstadoEscrituraRaza.RazaNoExiste;
        }

        if (await dbContext.Mascotas.AnyAsync(mascota => mascota.RazaId == id))
        {
            return EstadoEscrituraRaza.TieneMascotas;
        }

        dbContext.Razas.Remove(raza);
        try
        {
            await dbContext.SaveChangesAsync();
            return EstadoEscrituraRaza.Completada;
        }
        catch (DbUpdateException)
        {
            if (await dbContext.Mascotas.AnyAsync(mascota => mascota.RazaId == id))
            {
                return EstadoEscrituraRaza.TieneMascotas;
            }

            throw;
        }
    }
}
