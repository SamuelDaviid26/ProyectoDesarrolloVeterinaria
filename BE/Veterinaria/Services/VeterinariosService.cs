using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.DTOs.Veterinarios;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Services;

public enum EstadoVeterinario
{
    Completado,
    NoExiste,
    Conflicto,
    DatosInvalidos
}

public sealed record ResultadoVeterinario(
    EstadoVeterinario Estado,
    VeterinarioResponse? Veterinario = null,
    string? Error = null);

public sealed record FotoVeterinario(string TipoContenido, byte[] Contenido);

public class VeterinariosService(VeterinariaDbContext dbContext)
{
    public async Task<IReadOnlyList<VeterinarioResponse>> ListarAsync(string? nombre, string? numeroColegiado)
    {
        var consulta = dbContext.Veterinarios.AsNoTracking().AsQueryable();
        var filtroNombre = nombre?.Trim();
        var filtroColegiado = numeroColegiado?.Trim();

        if (!string.IsNullOrEmpty(filtroNombre))
        {
            consulta = consulta.Where(veterinario => veterinario.NombreCompleto.Contains(filtroNombre));
        }

        if (!string.IsNullOrEmpty(filtroColegiado))
        {
            consulta = consulta.Where(veterinario => veterinario.NumeroColegiado.Contains(filtroColegiado));
        }

        var filas = await consulta
            .OrderBy(veterinario => veterinario.NombreCompleto)
            .ThenBy(veterinario => veterinario.Id)
            .Select(veterinario => new
            {
                veterinario.Id,
                veterinario.NombreCompleto,
                veterinario.NumeroColegiado,
                veterinario.FotoId
            })
            .ToListAsync();

        return filas.Select(fila => new VeterinarioResponse(
            fila.Id,
            fila.NombreCompleto,
            fila.NumeroColegiado,
            fila.FotoId.HasValue ? $"/api/veterinarios/{fila.Id}/foto" : null)).ToArray();
    }

    public Task<VeterinarioResponse?> ObtenerAsync(int id)
    {
        return dbContext.Veterinarios
            .AsNoTracking()
            .Where(veterinario => veterinario.Id == id)
            .Select(veterinario => new VeterinarioResponse(
                veterinario.Id,
                veterinario.NombreCompleto,
                veterinario.NumeroColegiado,
                veterinario.FotoId.HasValue ? $"/api/veterinarios/{veterinario.Id}/foto" : null))
            .SingleOrDefaultAsync();
    }

    public Task<FotoVeterinario?> ObtenerFotoAsync(int id)
    {
        return dbContext.Veterinarios
            .AsNoTracking()
            .Where(veterinario => veterinario.Id == id)
            .Select(veterinario => veterinario.Foto == null
                ? null
                : new FotoVeterinario(veterinario.Foto.TipoContenido, veterinario.Foto.Contenido))
            .SingleOrDefaultAsync();
    }

    public async Task<ResultadoVeterinario> CrearAsync(CrearVeterinarioRequest request)
    {
        var validacionFoto = await ValidarFotoAsync(request.Foto!);
        if (validacionFoto.Archivo is null)
        {
            return new ResultadoVeterinario(EstadoVeterinario.DatosInvalidos, Error: validacionFoto.Error);
        }

        var veterinario = new Veterinario
        {
            NombreCompleto = request.NombreCompleto.Trim(),
            NumeroColegiado = request.NumeroColegiado.Trim(),
            Foto = validacionFoto.Archivo
        };

        await using var transaccion = await dbContext.Database.BeginTransactionAsync();
        dbContext.Veterinarios.Add(veterinario);
        await dbContext.SaveChangesAsync();
        await transaccion.CommitAsync();

        return new ResultadoVeterinario(EstadoVeterinario.Completado, ARespuesta(veterinario));
    }

    public async Task<ResultadoVeterinario> ActualizarAsync(int id, ActualizarVeterinarioRequest request)
    {
        Archivo? nuevaFoto = null;
        if (request.Foto is not null)
        {
            var validacionFoto = await ValidarFotoAsync(request.Foto);
            if (validacionFoto.Archivo is null)
            {
                return new ResultadoVeterinario(EstadoVeterinario.DatosInvalidos, Error: validacionFoto.Error);
            }

            nuevaFoto = validacionFoto.Archivo;
        }

        await using var transaccion = await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var veterinario = await dbContext.Veterinarios
            .Include(elemento => elemento.Foto)
            .SingleOrDefaultAsync(elemento => elemento.Id == id);
        if (veterinario is null)
        {
            return new ResultadoVeterinario(EstadoVeterinario.NoExiste);
        }

        veterinario.NombreCompleto = request.NombreCompleto.Trim();
        veterinario.NumeroColegiado = request.NumeroColegiado.Trim();

        if (nuevaFoto is not null)
        {
            var fotoAnterior = veterinario.Foto;
            dbContext.Archivos.Add(nuevaFoto);
            veterinario.Foto = nuevaFoto;

            if (fotoAnterior is not null &&
                !await ArchivoTieneOtrasReferenciasAsync(fotoAnterior.Id, veterinario.Id))
            {
                dbContext.Archivos.Remove(fotoAnterior);
            }
        }

        await dbContext.SaveChangesAsync();
        await transaccion.CommitAsync();
        return new ResultadoVeterinario(EstadoVeterinario.Completado, ARespuesta(veterinario));
    }

    public async Task<EstadoVeterinario> EliminarAsync(int id)
    {
        try
        {
            await using var transaccion = await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var veterinario = await dbContext.Veterinarios
                .Include(elemento => elemento.Foto)
                .SingleOrDefaultAsync(elemento => elemento.Id == id);
            if (veterinario is null)
            {
                return EstadoVeterinario.NoExiste;
            }

            if (await TieneReferenciasClinicasAsync(id))
            {
                return EstadoVeterinario.Conflicto;
            }

            var foto = veterinario.Foto;
            dbContext.Veterinarios.Remove(veterinario);

            if (foto is not null && !await ArchivoTieneOtrasReferenciasAsync(foto.Id, id))
            {
                dbContext.Archivos.Remove(foto);
            }

            await dbContext.SaveChangesAsync();
            await transaccion.CommitAsync();
            return EstadoVeterinario.Completado;
        }
        catch (DbUpdateException)
        {
            if (await TieneReferenciasClinicasAsync(id))
            {
                return EstadoVeterinario.Conflicto;
            }

            throw;
        }
    }

    private async Task<bool> TieneReferenciasClinicasAsync(int veterinarioId)
    {
        return await dbContext.Users.AnyAsync(usuario => usuario.VeterinarioId == veterinarioId)
            || await dbContext.VacunasAplicadas.AnyAsync(vacuna => vacuna.VeterinarioResponsableId == veterinarioId)
            || await dbContext.AsignacionesClinicas.AnyAsync(asignacion =>
                asignacion.VeterinarioResponsableId == veterinarioId ||
                asignacion.VeterinarioSuspensionResponsableId == veterinarioId)
            || await dbContext.Examenes.AnyAsync(examen => examen.VeterinarioResponsableId == veterinarioId)
            || await dbContext.NotasClinicas.AnyAsync(nota => nota.VeterinarioResponsableId == veterinarioId);
    }

    private async Task<bool> ArchivoTieneOtrasReferenciasAsync(int archivoId, int veterinarioId)
    {
        return await dbContext.Veterinarios.AnyAsync(veterinario =>
                veterinario.Id != veterinarioId && veterinario.FotoId == archivoId)
            || await dbContext.Mascotas.AnyAsync(mascota => mascota.FotoId == archivoId)
            || await dbContext.Examenes.AnyAsync(examen => examen.ArchivoId == archivoId);
    }

    private static VeterinarioResponse ARespuesta(Veterinario veterinario)
    {
        return new VeterinarioResponse(
            veterinario.Id,
            veterinario.NombreCompleto,
            veterinario.NumeroColegiado,
            veterinario.FotoId.HasValue ? $"/api/veterinarios/{veterinario.Id}/foto" : null);
    }

    private static async Task<(Archivo? Archivo, string? Error)> ValidarFotoAsync(IFormFile foto)
    {
        if (foto.Length == 0)
        {
            return (null, "La foto no puede estar vacía.");
        }

        var nombreOriginal = Path.GetFileName(foto.FileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(nombreOriginal) || nombreOriginal.Length > 260)
        {
            return (null, "El nombre del archivo es inválido o supera los 260 caracteres.");
        }

        await using var stream = new MemoryStream();
        await foto.CopyToAsync(stream);
        var contenido = stream.ToArray();
        var extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();

        var esJpeg = extension is ".jpg" or ".jpeg" && EsJpeg(contenido);
        var esPng = extension == ".png" && EsPng(contenido);
        if (!esJpeg && !esPng)
        {
            return (null, "La foto debe ser un archivo JPEG o PNG válido y coincidir con su extensión.");
        }

        return (new Archivo
        {
            NombreOriginal = nombreOriginal,
            TipoContenido = esJpeg ? "image/jpeg" : "image/png",
            Contenido = contenido
        }, null);
    }

    private static bool EsJpeg(byte[] contenido)
    {
        return contenido.Length >= 5 &&
            contenido[0] == 0xFF && contenido[1] == 0xD8 && contenido[2] == 0xFF &&
            contenido[^2] == 0xFF && contenido[^1] == 0xD9;
    }

    private static bool EsPng(byte[] contenido)
    {
        ReadOnlySpan<byte> firma = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        ReadOnlySpan<byte> marcadorFin = [0, 0, 0, 0, 0x49, 0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82];
        return contenido.Length >= 33 &&
            contenido.AsSpan(0, firma.Length).SequenceEqual(firma) &&
            contenido.AsSpan(12, 4).SequenceEqual("IHDR"u8) &&
            contenido.AsSpan(contenido.Length - marcadorFin.Length).SequenceEqual(marcadorFin);
    }
}
