using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Data;

public class VeterinariaDbContext(DbContextOptions<VeterinariaDbContext> options)
    : IdentityDbContext<Usuario>(options)
{
    public DbSet<Especie> Especies => Set<Especie>();
    public DbSet<Raza> Razas => Set<Raza>();
    public DbSet<Veterinario> Veterinarios => Set<Veterinario>();
    public DbSet<Mascota> Mascotas => Set<Mascota>();
    public DbSet<Archivo> Archivos => Set<Archivo>();
    public DbSet<ElementoCatalogoClinico> ElementosCatalogoClinico => Set<ElementoCatalogoClinico>();
    public DbSet<VacunaAplicada> VacunasAplicadas => Set<VacunaAplicada>();
    public DbSet<AsignacionClinica> AsignacionesClinicas => Set<AsignacionClinica>();
    public DbSet<Examen> Examenes => Set<Examen>();
    public DbSet<NotaClinica> NotasClinicas => Set<NotaClinica>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Usuario>(entity =>
        {
            entity.Property(usuario => usuario.Cedula).HasMaxLength(32).IsRequired();
            entity.HasIndex(usuario => usuario.Cedula).IsUnique();
            entity.HasIndex(usuario => usuario.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .IsUnique();
            entity.HasIndex(usuario => usuario.VeterinarioId)
                .IsUnique()
                .HasFilter("[VeterinarioId] IS NOT NULL");
            entity.HasOne(usuario => usuario.Veterinario)
                .WithMany()
                .HasForeignKey(usuario => usuario.VeterinarioId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Especie>(entity =>
        {
            entity.Property(especie => especie.Nombre).HasMaxLength(100).IsRequired();
        });

        builder.Entity<Raza>(entity =>
        {
            entity.Property(raza => raza.Nombre).HasMaxLength(100).IsRequired();
            entity.HasOne(raza => raza.Especie)
                .WithMany()
                .HasForeignKey(raza => raza.EspecieId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Veterinario>(entity =>
        {
            entity.Property(veterinario => veterinario.NombreCompleto).HasMaxLength(200).IsRequired();
            entity.Property(veterinario => veterinario.NumeroColegiado).HasMaxLength(50).IsRequired();
            entity.HasOne(veterinario => veterinario.Foto)
                .WithMany()
                .HasForeignKey(veterinario => veterinario.FotoId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Mascota>(entity =>
        {
            entity.Property(mascota => mascota.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(mascota => mascota.Sexo).HasMaxLength(30).IsRequired();
            entity.HasOne(mascota => mascota.Raza)
                .WithMany()
                .HasForeignKey(mascota => mascota.RazaId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(mascota => mascota.Dueno)
                .WithMany()
                .HasForeignKey(mascota => mascota.DuenoId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(mascota => mascota.Foto)
                .WithMany()
                .HasForeignKey(mascota => mascota.FotoId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Archivo>(entity =>
        {
            entity.Property(archivo => archivo.NombreOriginal).HasMaxLength(260).IsRequired();
            entity.Property(archivo => archivo.TipoContenido).HasMaxLength(150).IsRequired();
            entity.Property(archivo => archivo.Contenido).HasColumnType("varbinary(max)").IsRequired();
        });

        builder.Entity<ElementoCatalogoClinico>(entity =>
        {
            entity.Property(elemento => elemento.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(elemento => elemento.Descripcion).HasMaxLength(1000).IsRequired();
            entity.Property(elemento => elemento.Tipo).HasConversion<string>().HasMaxLength(30);
        });

        builder.Entity<VacunaAplicada>(entity =>
        {
            entity.Property(vacuna => vacuna.NombreVacuna).HasMaxLength(150).IsRequired();
            entity.HasIndex(vacuna => vacuna.VacunaAnteriorId)
                .IsUnique()
                .HasFilter("[VacunaAnteriorId] IS NOT NULL");
            entity.HasOne(vacuna => vacuna.Mascota)
                .WithMany()
                .HasForeignKey(vacuna => vacuna.MascotaId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(vacuna => vacuna.VacunaAnterior)
                .WithMany()
                .HasForeignKey(vacuna => vacuna.VacunaAnteriorId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(vacuna => vacuna.VeterinarioResponsable)
                .WithMany()
                .HasForeignKey(vacuna => vacuna.VeterinarioResponsableId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(vacuna => vacuna.RegistradoPor)
                .WithMany()
                .HasForeignKey(vacuna => vacuna.RegistradoPorId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<AsignacionClinica>(entity =>
        {
            entity.HasOne(asignacion => asignacion.Mascota)
                .WithMany()
                .HasForeignKey(asignacion => asignacion.MascotaId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(asignacion => asignacion.ElementoCatalogoClinico)
                .WithMany()
                .HasForeignKey(asignacion => asignacion.ElementoCatalogoClinicoId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(asignacion => asignacion.VeterinarioResponsable)
                .WithMany()
                .HasForeignKey(asignacion => asignacion.VeterinarioResponsableId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(asignacion => asignacion.RegistradoPor)
                .WithMany()
                .HasForeignKey(asignacion => asignacion.RegistradoPorId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(asignacion => asignacion.VeterinarioSuspensionResponsable)
                .WithMany()
                .HasForeignKey(asignacion => asignacion.VeterinarioSuspensionResponsableId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(asignacion => asignacion.SuspendidoPor)
                .WithMany()
                .HasForeignKey(asignacion => asignacion.SuspendidoPorId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Examen>(entity =>
        {
            entity.Property(examen => examen.Descripcion).HasMaxLength(1000).IsRequired();
            entity.HasOne(examen => examen.Mascota)
                .WithMany()
                .HasForeignKey(examen => examen.MascotaId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(examen => examen.Archivo)
                .WithMany()
                .HasForeignKey(examen => examen.ArchivoId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(examen => examen.VeterinarioResponsable)
                .WithMany()
                .HasForeignKey(examen => examen.VeterinarioResponsableId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(examen => examen.RegistradoPor)
                .WithMany()
                .HasForeignKey(examen => examen.RegistradoPorId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<NotaClinica>(entity =>
        {
            entity.Property(nota => nota.ContenidoHtml).IsRequired();
            entity.HasOne(nota => nota.Mascota)
                .WithMany()
                .HasForeignKey(nota => nota.MascotaId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(nota => nota.VeterinarioResponsable)
                .WithMany()
                .HasForeignKey(nota => nota.VeterinarioResponsableId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(nota => nota.RegistradoPor)
                .WithMany()
                .HasForeignKey(nota => nota.RegistradoPorId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}
