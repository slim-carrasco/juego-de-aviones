using JuegoAviones.Core.Entidades;
using Microsoft.EntityFrameworkCore;

namespace JuegoAviones.Data.Conexion;

public sealed class JuegoDbContext(DbContextOptions<JuegoDbContext> options) : DbContext(options)
{
    public DbSet<Partida> Partidas => Set<Partida>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<PartidaEquipo> PartidasEquipo => Set<PartidaEquipo>();
    public DbSet<DetalleJugadorPartida> DetallesJugadorPartida => Set<DetalleJugadorPartida>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Partida>(entity =>
        {
            entity.ToTable("partidas");
            entity.HasKey(partida => partida.Id);
            entity.Property(partida => partida.Jugador).HasMaxLength(50).IsRequired();
            entity.Property(partida => partida.Oponente).HasMaxLength(50);
            entity.Property(partida => partida.Modo).HasMaxLength(24).IsRequired();
            entity.Property(partida => partida.Resultado).HasConversion<string>().IsRequired();
            entity.Property(partida => partida.FechaUtc).IsRequired();
            entity.HasIndex(partida => new { partida.Jugador, partida.Resultado });
        });
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");
            entity.HasKey(usuario => usuario.Id);
            entity.Property(usuario => usuario.Nombre).HasMaxLength(30).IsRequired();
            entity.HasIndex(usuario => usuario.Nombre).IsUnique();
            entity.Property(usuario => usuario.ClaveHash).HasMaxLength(150).IsRequired();
            entity.Property(usuario => usuario.FechaUtc).IsRequired();
        });
        modelBuilder.Entity<PartidaEquipo>(entity =>
        {
            entity.ToTable("partidas_equipo");
            entity.HasKey(partida => partida.Id);
            entity.Property(partida => partida.EquipoGanador).HasConversion<string>().HasMaxLength(8).IsRequired();
            entity.Property(partida => partida.DuracionSegundos).IsRequired();
            entity.Property(partida => partida.FechaUtc).IsRequired();
            entity.HasMany(partida => partida.Detalles).WithOne().HasForeignKey(detalle => detalle.PartidaEquipoId);
        });
        modelBuilder.Entity<DetalleJugadorPartida>(entity =>
        {
            entity.ToTable("detalle_jugador_partida");
            entity.HasKey(detalle => detalle.Id);
            entity.Property(detalle => detalle.Usuario).HasMaxLength(30).IsRequired();
            entity.Property(detalle => detalle.Equipo).HasConversion<string>().HasMaxLength(8).IsRequired();
            entity.Property(detalle => detalle.Avion).HasConversion<string>().HasMaxLength(8).IsRequired();
            entity.HasIndex(detalle => new { detalle.PartidaEquipoId, detalle.Usuario });
        });
    }
}
