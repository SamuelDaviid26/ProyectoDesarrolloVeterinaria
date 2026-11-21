using System.Data;
using AngleSharp.Html.Parser;
using Ganss.Xss;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.DTOs.Expedientes;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Services;

public enum EstadoExpediente { Completado, NoExiste, DatosInvalidos, Conflicto }
public sealed record ResultadoExpediente<T>(EstadoExpediente Estado, T? Valor = default, string? Error = null);
public sealed record ArchivoDescarga(string Nombre, string TipoContenido, byte[] Contenido);
public sealed record ResultadoSuspension(EstadoExpediente Estado, string? Error = null);

public class ExpedientesService(VeterinariaDbContext dbContext, UserManager<Usuario> userManager)
{
    private const string RolAdministrador = "Administrador";
    private const string RolVeterinario = "Veterinario";
    private readonly HtmlSanitizer sanitizer = CrearSanitizer();

    public async Task<IReadOnlyList<VacunaResponse>?> ListarVacunasAsync(int mascotaId)
    {
        if (!await MascotaExisteAsync(mascotaId)) return null;
        var hoyCostaRica = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-6));
        var vacunas = await dbContext.VacunasAplicadas.AsNoTracking().Where(v => v.MascotaId == mascotaId)
            .OrderByDescending(v => v.RegistradoEn).ThenByDescending(v => v.Id)
            .Select(v => new
            {
                v.Id, v.NombreVacuna, v.FechaAplicacion, v.FechaProximaDosis, v.RegistradoEn,
                v.VacunaAnteriorId, v.VeterinarioResponsableId,
                Veterinario = v.VeterinarioResponsable!.NombreCompleto,
                v.RegistradoPorId, RegistradoPor = v.RegistradoPor!.NombreCompleto,
                Cumplida = dbContext.VacunasAplicadas.Any(siguiente => siguiente.VacunaAnteriorId == v.Id)
            }).ToListAsync();
        return vacunas.Select(v => new VacunaResponse(v.Id, v.NombreVacuna, v.FechaAplicacion,
            v.FechaProximaDosis, v.RegistradoEn, v.VacunaAnteriorId, v.VeterinarioResponsableId,
            v.Veterinario, v.RegistradoPorId, v.RegistradoPor,
            !v.FechaProximaDosis.HasValue ? null : v.Cumplida ? "Cumplida" :
                v.FechaProximaDosis.Value < hoyCostaRica ? "Vencida" : "Pendiente")).ToArray();
    }

    public async Task<ResultadoExpediente<VacunaResponse>> CrearVacunaAsync(
        int mascotaId, string actorId, CrearVacunaRequest request)
    {
        var nombre = request.NombreVacuna.Trim();
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 150 || request.FechaAplicacion == default ||
            request.FechaProximaDosis < request.FechaAplicacion)
            return Invalid<VacunaResponse>("Los datos de la vacuna o sus fechas no son válidos.");
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await MascotaExisteAsync(mascotaId)) return Missing<VacunaResponse>();
        if (request.VacunaAnteriorId is int anteriorId)
        {
            var anterior = await dbContext.VacunasAplicadas.SingleOrDefaultAsync(v => v.Id == anteriorId && v.MascotaId == mascotaId);
            if (anterior is null || !anterior.FechaProximaDosis.HasValue ||
                await dbContext.VacunasAplicadas.AnyAsync(v => v.VacunaAnteriorId == anteriorId))
                return Conflict<VacunaResponse>("La vacuna anterior no pertenece a la mascota o ya fue cumplida.");
        }
        var autor = await ResolverAutorAsync(actorId, request.VeterinarioResponsableId);
        if (autor is null) return Invalid<VacunaResponse>("No se pudo resolver un veterinario responsable válido.");
        var entidad = new VacunaAplicada
        {
            MascotaId = mascotaId, NombreVacuna = nombre, FechaAplicacion = request.FechaAplicacion,
            FechaProximaDosis = request.FechaProximaDosis, VacunaAnteriorId = request.VacunaAnteriorId,
            RegistradoEn = DateTimeOffset.UtcNow, VeterinarioResponsableId = autor.Value.VeterinarioId,
            RegistradoPorId = actorId
        };
        dbContext.VacunasAplicadas.Add(entidad);
        try { await dbContext.SaveChangesAsync(); }
        catch (DbUpdateException) when (request.VacunaAnteriorId.HasValue)
        {
            return Conflict<VacunaResponse>("La dosis pendiente ya fue cumplida por otra aplicación.");
        }
        await tx.CommitAsync();
        var response = await dbContext.VacunasAplicadas.AsNoTracking().Where(v => v.Id == entidad.Id)
            .Select(v => new VacunaResponse(v.Id, v.NombreVacuna, v.FechaAplicacion, v.FechaProximaDosis,
                v.RegistradoEn, v.VacunaAnteriorId, v.VeterinarioResponsableId,
                v.VeterinarioResponsable!.NombreCompleto, v.RegistradoPorId,
                v.RegistradoPor!.NombreCompleto,
                v.FechaProximaDosis.HasValue && v.FechaProximaDosis.Value < DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-6))
                    ? "Vencida" : v.FechaProximaDosis.HasValue ? "Pendiente" : null)).SingleAsync();
        return new(EstadoExpediente.Completado, response);
    }

    public async Task<TratamientosResponse?> ListarTratamientosAsync(int mascotaId)
    {
        if (!await MascotaExisteAsync(mascotaId)) return null;
        var elementos = await dbContext.AsignacionesClinicas.AsNoTracking().Where(a => a.MascotaId == mascotaId)
            .OrderByDescending(a => a.AsignadoEn).ThenByDescending(a => a.Id)
            .Select(a => new TratamientoResponse(a.Id, a.ElementoCatalogoClinicoId,
                a.ElementoCatalogoClinico!.Nombre, a.ElementoCatalogoClinico.Descripcion,
                a.ElementoCatalogoClinico.Tipo.ToString(), a.AsignadoEn, a.VeterinarioResponsableId,
                a.VeterinarioResponsable!.NombreCompleto, a.RegistradoPorId, a.RegistradoPor!.NombreCompleto,
                a.SuspendidoEn, a.VeterinarioSuspensionResponsableId,
                a.VeterinarioSuspensionResponsable == null ? null : a.VeterinarioSuspensionResponsable.NombreCompleto,
                a.SuspendidoPorId, a.SuspendidoPor == null ? null : a.SuspendidoPor.NombreCompleto))
            .ToListAsync();
        return new(elementos.Where(a => !a.SuspendidoEn.HasValue).ToArray(),
            elementos.Where(a => a.SuspendidoEn.HasValue).ToArray());
    }

    public async Task<ResultadoExpediente<TratamientoResponse>> CrearTratamientoAsync(
        int mascotaId, string actorId, CrearTratamientoRequest request)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await MascotaExisteAsync(mascotaId)) return Missing<TratamientoResponse>();
        var elemento = await dbContext.ElementosCatalogoClinico.AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == request.ElementoCatalogoClinicoId);
        if (elemento is null) return Invalid<TratamientoResponse>("El elemento del catálogo no existe.");
        var autor = await ResolverAutorAsync(actorId, request.VeterinarioResponsableId);
        if (autor is null) return Invalid<TratamientoResponse>("No se pudo resolver un veterinario responsable válido.");
        var entidad = new AsignacionClinica
        {
            MascotaId = mascotaId, ElementoCatalogoClinicoId = elemento.Id,
            AsignadoEn = DateTimeOffset.UtcNow, VeterinarioResponsableId = autor.Value.VeterinarioId,
            RegistradoPorId = actorId
        };
        dbContext.AsignacionesClinicas.Add(entidad);
        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();
        var response = await dbContext.AsignacionesClinicas.AsNoTracking().Where(a => a.Id == entidad.Id)
            .Select(a => new TratamientoResponse(a.Id, a.ElementoCatalogoClinicoId,
                a.ElementoCatalogoClinico!.Nombre, a.ElementoCatalogoClinico.Descripcion,
                a.ElementoCatalogoClinico.Tipo.ToString(), a.AsignadoEn, a.VeterinarioResponsableId,
                a.VeterinarioResponsable!.NombreCompleto, a.RegistradoPorId, a.RegistradoPor!.NombreCompleto,
                a.SuspendidoEn, a.VeterinarioSuspensionResponsableId, null, a.SuspendidoPorId, null)).SingleAsync();
        return new(EstadoExpediente.Completado, response);
    }

    public async Task<ResultadoSuspension> SuspenderTratamientoAsync(
        int mascotaId, int asignacionId, string actorId, SuspenderTratamientoRequest request)
    {
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await MascotaExisteAsync(mascotaId)) return new(EstadoExpediente.NoExiste);
        var asignacion = await dbContext.AsignacionesClinicas.SingleOrDefaultAsync(a => a.Id == asignacionId && a.MascotaId == mascotaId);
        if (asignacion is null) return new(EstadoExpediente.NoExiste);
        if (asignacion.SuspendidoEn.HasValue) return new(EstadoExpediente.Conflicto, "El tratamiento ya está suspendido.");
        var autor = await ResolverAutorAsync(actorId, request.VeterinarioResponsableId);
        if (autor is null) return new(EstadoExpediente.DatosInvalidos, "No se pudo resolver un veterinario responsable válido.");
        asignacion.SuspendidoEn = DateTimeOffset.UtcNow;
        asignacion.VeterinarioSuspensionResponsableId = autor.Value.VeterinarioId;
        asignacion.SuspendidoPorId = actorId;
        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();
        return new(EstadoExpediente.Completado);
    }

    public async Task<IReadOnlyList<ExamenResponse>?> ListarExamenesAsync(int mascotaId)
    {
        if (!await MascotaExisteAsync(mascotaId)) return null;
        return await dbContext.Examenes.AsNoTracking().Where(e => e.MascotaId == mascotaId)
            .OrderByDescending(e => e.RegistradoEn).ThenByDescending(e => e.Id)
            .Select(e => new ExamenResponse(e.Id, e.Descripcion, e.RegistradoEn, e.ArchivoId,
                e.Archivo!.NombreOriginal, e.Archivo.TipoContenido, $"/api/mascotas/{mascotaId}/examenes/{e.Id}/archivo",
                e.VeterinarioResponsableId, e.VeterinarioResponsable!.NombreCompleto,
                e.RegistradoPorId, e.RegistradoPor!.NombreCompleto)).ToListAsync();
    }

    public async Task<ResultadoExpediente<ExamenResponse>> CrearExamenAsync(
        int mascotaId, string actorId, CrearExamenRequest request)
    {
        var descripcion = request.Descripcion.Trim();
        if (string.IsNullOrWhiteSpace(descripcion) || descripcion.Length > 1000)
            return Invalid<ExamenResponse>("La descripción es obligatoria y no puede superar 1000 caracteres.");
        var validacion = await ValidarArchivoAsync(request.Archivo!);
        if (validacion.Archivo is null) return Invalid<ExamenResponse>(validacion.Error!);
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await MascotaExisteAsync(mascotaId)) return Missing<ExamenResponse>();
        var autor = await ResolverAutorAsync(actorId, request.VeterinarioResponsableId);
        if (autor is null) return Invalid<ExamenResponse>("No se pudo resolver un veterinario responsable válido.");
        var examen = new Examen
        {
            MascotaId = mascotaId, Descripcion = descripcion, Archivo = validacion.Archivo,
            RegistradoEn = DateTimeOffset.UtcNow, VeterinarioResponsableId = autor.Value.VeterinarioId,
            RegistradoPorId = actorId
        };
        dbContext.Examenes.Add(examen);
        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();
        var response = await dbContext.Examenes.AsNoTracking().Where(e => e.Id == examen.Id)
            .Select(e => new ExamenResponse(e.Id, e.Descripcion, e.RegistradoEn, e.ArchivoId,
                e.Archivo!.NombreOriginal, e.Archivo.TipoContenido, $"/api/mascotas/{mascotaId}/examenes/{e.Id}/archivo",
                e.VeterinarioResponsableId, e.VeterinarioResponsable!.NombreCompleto,
                e.RegistradoPorId, e.RegistradoPor!.NombreCompleto)).SingleAsync();
        return new(EstadoExpediente.Completado, response);
    }

    public async Task<ArchivoDescarga?> ObtenerArchivoExamenAsync(int mascotaId, int examenId)
    {
        if (!await MascotaExisteAsync(mascotaId)) return null;
        return await dbContext.Examenes.AsNoTracking().Where(e => e.Id == examenId && e.MascotaId == mascotaId)
            .Select(e => new ArchivoDescarga(e.Archivo!.NombreOriginal, e.Archivo.TipoContenido, e.Archivo.Contenido))
            .SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<NotaResponse>?> ListarNotasAsync(int mascotaId)
    {
        if (!await MascotaExisteAsync(mascotaId)) return null;
        return await dbContext.NotasClinicas.AsNoTracking().Where(n => n.MascotaId == mascotaId)
            .OrderByDescending(n => n.RegistradoEn).ThenByDescending(n => n.Id)
            .Select(n => new NotaResponse(n.Id, n.ContenidoHtml, n.RegistradoEn,
                n.VeterinarioResponsableId, n.VeterinarioResponsable!.NombreCompleto,
                n.RegistradoPorId, n.RegistradoPor!.NombreCompleto)).ToListAsync();
    }

    public async Task<ResultadoExpediente<NotaResponse>> CrearNotaAsync(
        int mascotaId, string actorId, CrearNotaRequest request)
    {
        var html = sanitizer.Sanitize(request.ContenidoHtml ?? string.Empty);
        var texto = new HtmlParser().ParseDocument(html).Body?.TextContent;
        if (string.IsNullOrWhiteSpace(texto)) return Invalid<NotaResponse>("La nota no puede quedar vacía después del saneo.");
        await using var tx = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await MascotaExisteAsync(mascotaId)) return Missing<NotaResponse>();
        var autor = await ResolverAutorAsync(actorId, request.VeterinarioResponsableId);
        if (autor is null) return Invalid<NotaResponse>("No se pudo resolver un veterinario responsable válido.");
        var nota = new NotaClinica
        {
            MascotaId = mascotaId, ContenidoHtml = html, RegistradoEn = DateTimeOffset.UtcNow,
            VeterinarioResponsableId = autor.Value.VeterinarioId, RegistradoPorId = actorId
        };
        dbContext.NotasClinicas.Add(nota);
        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();
        var response = await dbContext.NotasClinicas.AsNoTracking().Where(n => n.Id == nota.Id)
            .Select(n => new NotaResponse(n.Id, n.ContenidoHtml, n.RegistradoEn,
                n.VeterinarioResponsableId, n.VeterinarioResponsable!.NombreCompleto,
                n.RegistradoPorId, n.RegistradoPor!.NombreCompleto)).SingleAsync();
        return new(EstadoExpediente.Completado, response);
    }

    private async Task<(Usuario Usuario, int VeterinarioId)?> ResolverAutorAsync(string actorId, int? veterinarioResponsableId)
    {
        var usuario = await userManager.FindByIdAsync(actorId);
        if (usuario is null || usuario.Bloqueado) return null;
        if (await userManager.IsInRoleAsync(usuario, RolVeterinario))
        {
            if (usuario.VeterinarioId is not int id || !await dbContext.Veterinarios.AnyAsync(v => v.Id == id)) return null;
            return (usuario, id);
        }
        if (!await userManager.IsInRoleAsync(usuario, RolAdministrador) || veterinarioResponsableId is not int vetId ||
            !await dbContext.Veterinarios.AnyAsync(v => v.Id == vetId)) return null;
        return (usuario, vetId);
    }

    private Task<bool> MascotaExisteAsync(int id) => dbContext.Mascotas.AnyAsync(m => m.Id == id);

    private static ResultadoExpediente<T> Invalid<T>(string error) => new(EstadoExpediente.DatosInvalidos, Error: error);
    private static ResultadoExpediente<T> Missing<T>() => new(EstadoExpediente.NoExiste);
    private static ResultadoExpediente<T> Conflict<T>(string error) => new(EstadoExpediente.Conflicto, Error: error);

    private static HtmlSanitizer CrearSanitizer()
    {
        var instance = new HtmlSanitizer();
        instance.AllowedTags.Clear();
        instance.AllowedTags.UnionWith(["p", "br", "b", "strong", "i", "em", "ul", "ol", "li"]);
        instance.AllowedAttributes.Clear();
        instance.AllowedCssProperties.Clear();
        instance.AllowedSchemes.Clear();
        return instance;
    }

    private static async Task<(Archivo? Archivo, string? Error)> ValidarArchivoAsync(IFormFile archivo)
    {
        if (archivo.Length == 0) return (null, "El archivo está vacío.");
        var nombre = Path.GetFileName(archivo.FileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 260) return (null, "El nombre del archivo no es válido.");
        await using var stream = new MemoryStream();
        await archivo.CopyToAsync(stream);
        var bytes = stream.ToArray();
        var extension = Path.GetExtension(nombre).ToLowerInvariant();
        var jpeg = extension is ".jpg" or ".jpeg" && bytes.Length >= 5 && bytes[0] == 0xff && bytes[1] == 0xd8 &&
            bytes[2] == 0xff && bytes[^2] == 0xff && bytes[^1] == 0xd9;
        ReadOnlySpan<byte> pngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
        ReadOnlySpan<byte> pngEnd = [0, 0, 0, 0, 0x49, 0x45, 0x4e, 0x44, 0xae, 0x42, 0x60, 0x82];
        var png = extension == ".png" && bytes.Length >= 33 && bytes.AsSpan(0, 8).SequenceEqual(pngSignature) &&
            bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8) && bytes.AsSpan(bytes.Length - 12).SequenceEqual(pngEnd);
        var pdf = extension == ".pdf" && bytes.Length >= 10 && bytes.AsSpan(0, 5).SequenceEqual("%PDF-"u8) &&
            System.Text.Encoding.ASCII.GetString(bytes).TrimEnd().EndsWith("%%EOF", StringComparison.Ordinal);
        if (!jpeg && !png && !pdf) return (null, "El archivo debe ser JPEG, PNG o PDF válido y coincidir con su extensión.");
        return (new Archivo { NombreOriginal = nombre, TipoContenido = jpeg ? "image/jpeg" : png ? "image/png" : "application/pdf", Contenido = bytes }, null);
    }
}
