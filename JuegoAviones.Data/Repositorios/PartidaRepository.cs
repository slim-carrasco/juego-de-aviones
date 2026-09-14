using JuegoAviones.Core.Entidades;
using JuegoAviones.Core.Interfaces;
using JuegoAviones.Data.Conexion;
using Microsoft.EntityFrameworkCore;

namespace JuegoAviones.Data.Repositorios;

public sealed class PartidaRepository(JuegoDbContext db) : IPartidaRepository
{
    public async Task AgregarAsync(Partida partida, CancellationToken cancellationToken)
    {
        db.Partidas.Add(partida);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Partida?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        db.Partidas.AsNoTracking().SingleOrDefaultAsync(partida => partida.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Partida>> ObtenerTodasAsync(CancellationToken cancellationToken) =>
        await db.Partidas.AsNoTracking().OrderByDescending(partida => partida.FechaUtc).ToListAsync(cancellationToken);
}
