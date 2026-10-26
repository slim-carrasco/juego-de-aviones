using JuegoAviones.Core.Entidades;
using JuegoAviones.Core.Interfaces;
using JuegoAviones.Data.Conexion;

namespace JuegoAviones.Data.Repositorios;

public sealed class ResultadoEquipoRepository(JuegoDbContext db) : IResultadoEquipoRepository
{
    public async Task<int> GuardarAsync(PartidaEquipo partida, CancellationToken cancellationToken)
    {
        db.PartidasEquipo.Add(partida);
        await db.SaveChangesAsync(cancellationToken);
        return partida.Id;
    }
}
