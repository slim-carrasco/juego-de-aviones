using JuegoAviones.Core.Entidades;
using Microsoft.EntityFrameworkCore;

namespace JuegoAviones.Data.Conexion;

public sealed class JuegoDbContext(DbContextOptions<JuegoDbContext> options) : DbContext(options)
{
    public DbSet<Partida> Partidas => Set<Partida>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Partida>(entity =>
        {
            entity.ToTable("partidas");
            entity.HasKey(partida => partida.Id);
            entity.Property(partida => partida.Jugador).HasMaxLength(50).IsRequired();
            entity.Property(partida => partida.Resultado).HasConversion<string>().IsRequired();
            entity.Property(partida => partida.FechaUtc).IsRequired();
            entity.HasIndex(partida => new { partida.Jugador, partida.Resultado });
        });
    }
}
