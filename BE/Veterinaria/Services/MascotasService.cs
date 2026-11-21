using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.DTOs.Mascotas;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Services;

public enum EstadoMascota { Completado, NoExiste, DatosInvalidos }
public sealed record ResultadoMascota(EstadoMascota Estado, MascotaResponse? Mascota = null, string? Error = null);
public sealed record FotoMascota(string TipoContenido, byte[] Contenido);

public class MascotasService(VeterinariaDbContext dbContext)
{
    private const string RolDueno = "Dueño";

    public async Task<ResultadoMascota> CrearAsync(CrearMascotaRequest request)
    {
        var error = ValidarDatos(request);
        if (error is not null) return new(EstadoMascota.DatosInvalidos, Error: error);
        var archivo = await ValidarFotoAsync(request.Foto!);
        if (archivo.Archivo is null) return new(EstadoMascota.DatosInvalidos, Error: archivo.Error);
        if (!await dbContext.Razas.AnyAsync(r => r.Id == request.RazaId))
            return new(EstadoMascota.DatosInvalidos, Error: "La raza indicada no existe.");
        if (!await EsDuenoAsync(request.DuenoId))
            return new(EstadoMascota.DatosInvalidos, Error: "El dueño indicado no existe o no tiene el rol Dueño.");

        await using var tx = await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var mascota = new Mascota
        {
            Nombre = request.Nombre, RazaId = request.RazaId, FechaNacimiento = request.FechaNacimiento,
            Sexo = request.Sexo, DuenoId = request.DuenoId, Foto = archivo.Archivo
        };
        dbContext.Mascotas.Add(mascota);
        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();
        return new(EstadoMascota.Completado, await ObtenerAsync(mascota.Id));
    }

    public async Task<EstadoMascota> ActualizarAsync(int id, ActualizarMascotaRequest request)
    {
        var error = ValidarDatos(request);
        if (error is not null) return EstadoMascota.DatosInvalidos;
        Archivo? nuevaFoto = null;
        if (request.Foto is not null)
        {
            var archivo = await ValidarFotoAsync(request.Foto);
            if (archivo.Archivo is null) return EstadoMascota.DatosInvalidos;
            nuevaFoto = archivo.Archivo;
        }

        await using var tx = await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var mascota = await dbContext.Mascotas.Include(m => m.Foto).SingleOrDefaultAsync(m => m.Id == id);
        if (mascota is null) return EstadoMascota.NoExiste;
        if (!await dbContext.Razas.AnyAsync(r => r.Id == request.RazaId) || !await EsDuenoAsync(request.DuenoId))
            return EstadoMascota.DatosInvalidos;

        mascota.Nombre = request.Nombre;
        mascota.RazaId = request.RazaId;
        mascota.FechaNacimiento = request.FechaNacimiento;
        mascota.Sexo = request.Sexo;
        mascota.DuenoId = request.DuenoId;
        if (nuevaFoto is not null)
        {
            var fotoAnterior = mascota.Foto;
            dbContext.Archivos.Add(nuevaFoto);
            mascota.Foto = nuevaFoto;
            if (fotoAnterior is not null && !await ArchivoTieneOtrasReferenciasAsync(fotoAnterior.Id, id))
                dbContext.Archivos.Remove(fotoAnterior);
        }
        await dbContext.SaveChangesAsync();
        await tx.CommitAsync();
        return EstadoMascota.Completado;
    }

    public Task<MascotaResponse?> ObtenerAsync(int id) => dbContext.Mascotas.AsNoTracking()
        .Where(m => m.Id == id)
        .Select(m => new MascotaResponse(m.Id, m.Nombre, m.RazaId, m.Raza!.Nombre,
            m.Raza.EspecieId, m.Raza.Especie!.Nombre, m.FechaNacimiento, m.Sexo,
            m.DuenoId, m.Dueno!.NombreCompleto, m.Dueno.Cedula,
            m.FotoId.HasValue ? $"/api/mascotas/{m.Id}/foto" : null))
        .SingleOrDefaultAsync();

    public Task<FotoMascota?> ObtenerFotoAsync(int id, string? duenoId = null) => dbContext.Mascotas.AsNoTracking()
        .Where(m => m.Id == id && (duenoId == null || m.DuenoId == duenoId))
        .Select(m => m.Foto == null ? null : new FotoMascota(m.Foto.TipoContenido, m.Foto.Contenido))
        .SingleOrDefaultAsync();

    public async Task<IReadOnlyList<MiMascotaResponse>> ListarDelDuenoAsync(string duenoId) =>
        await dbContext.Mascotas.AsNoTracking().Where(m => m.DuenoId == duenoId)
            .OrderBy(m => m.Nombre).ThenBy(m => m.Id)
            .Select(m => new MiMascotaResponse(m.Id, m.Nombre, m.Raza!.Especie!.Nombre, m.Raza.Nombre,
                m.FotoId.HasValue ? $"/api/mis-mascotas/{m.Id}/foto" : null))
            .ToListAsync();

    public async Task<MascotasListadoResponse> ListarAsync(string? nombre, string? cedulaDueno)
    {
        var consulta = dbContext.Mascotas.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            var filtro = nombre.Trim();
            consulta = consulta.Where(m => m.Nombre.Contains(filtro));
        }
        if (!string.IsNullOrWhiteSpace(cedulaDueno))
        {
            var cedula = cedulaDueno.Trim();
            consulta = consulta.Where(m => m.Dueno!.Cedula == cedula);
        }

        var proyeccion = consulta.Select(m => new MascotaListadoResponse(
            m.Id, m.Nombre, m.DuenoId, m.Dueno!.NombreCompleto, m.Dueno.Cedula,
            m.Raza!.EspecieId, m.Raza.Especie!.Nombre, m.RazaId, m.Raza.Nombre,
            m.FechaNacimiento, m.FotoId.HasValue ? $"/api/mascotas/{m.Id}/foto" : null,
            dbContext.VacunasAplicadas.Where(x => x.MascotaId == m.Id).Select(x => (DateTimeOffset?)x.RegistradoEn)
                .Concat(dbContext.AsignacionesClinicas.Where(x => x.MascotaId == m.Id).Select(x => (DateTimeOffset?)x.AsignadoEn))
                .Concat(dbContext.Examenes.Where(x => x.MascotaId == m.Id).Select(x => (DateTimeOffset?)x.RegistradoEn))
                .Concat(dbContext.NotasClinicas.Where(x => x.MascotaId == m.Id).Select(x => (DateTimeOffset?)x.RegistradoEn))
                .OrderByDescending(x => x).FirstOrDefault()));

        var filas = await proyeccion.ToListAsync();
        return new MascotasListadoResponse(
            filas.Where(m => m.UltimaAtencion.HasValue).OrderByDescending(m => m.UltimaAtencion).ThenBy(m => m.Id).ToArray(),
            filas.Where(m => !m.UltimaAtencion.HasValue).OrderBy(m => m.Nombre).ThenBy(m => m.Id).ToArray());
    }

    private Task<bool> EsDuenoAsync(string id) =>
        (from user in dbContext.Users
         join ur in dbContext.UserRoles on user.Id equals ur.UserId
         join role in dbContext.Roles on ur.RoleId equals role.Id
         where user.Id == id && role.Name == RolDueno
         select user.Id).AnyAsync();

    private async Task<bool> ArchivoTieneOtrasReferenciasAsync(int archivoId, int mascotaId) =>
        await dbContext.Veterinarios.AnyAsync(v => v.FotoId == archivoId) ||
        await dbContext.Mascotas.AnyAsync(m => m.Id != mascotaId && m.FotoId == archivoId) ||
        await dbContext.Examenes.AnyAsync(e => e.ArchivoId == archivoId);

    private static string? ValidarDatos(MascotaRequestBase request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Length > 100)
            return "El nombre es obligatorio y no puede superar 100 caracteres.";
        if (string.IsNullOrWhiteSpace(request.Sexo) || request.Sexo.Length > 30)
            return "El sexo es obligatorio y no puede superar 30 caracteres.";
        if (string.IsNullOrWhiteSpace(request.DuenoId) || request.RazaId <= 0)
            return "Se requiere un dueño y una raza válidos.";
        if (request.FechaNacimiento == default || request.FechaNacimiento > DateOnly.FromDateTime(DateTime.Today))
            return "La fecha de nacimiento debe ser válida y no futura.";
        return null;
    }

    private static async Task<(Archivo? Archivo, string? Error)> ValidarFotoAsync(IFormFile foto)
    {
        if (foto.Length == 0) return (null, "La foto no puede estar vacía.");
        var nombre = Path.GetFileName(foto.FileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 260)
            return (null, "El nombre de archivo es inválido o supera 260 caracteres.");
        await using var stream = new MemoryStream();
        await foto.CopyToAsync(stream);
        var bytes = stream.ToArray();
        var ext = Path.GetExtension(nombre).ToLowerInvariant();
        var jpeg = ext is ".jpg" or ".jpeg" && bytes.Length >= 5 && bytes[0] == 0xff && bytes[1] == 0xd8 &&
            bytes[2] == 0xff && bytes[^2] == 0xff && bytes[^1] == 0xd9;
        ReadOnlySpan<byte> pngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
        ReadOnlySpan<byte> pngEnd = [0, 0, 0, 0, 0x49, 0x45, 0x4e, 0x44, 0xae, 0x42, 0x60, 0x82];
        var png = ext == ".png" && bytes.Length >= 33 && bytes.AsSpan(0, 8).SequenceEqual(pngSignature) &&
            bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8) && bytes.AsSpan(bytes.Length - 12).SequenceEqual(pngEnd);
        if (!jpeg && !png) return (null, "La foto debe ser JPEG o PNG y coincidir con su extensión.");
        return (new Archivo { NombreOriginal = nombre, TipoContenido = jpeg ? "image/jpeg" : "image/png", Contenido = bytes }, null);
    }
}
