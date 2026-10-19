using JuegoAviones.Core.Entidades;

namespace JuegoAviones.Core.Interfaces;

public interface IPartidaRepository
{
    Task AgregarAsync(Partida partida, CancellationToken cancellationToken);
    Task<Partida?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Partida>> ObtenerTodasAsync(CancellationToken cancellationToken);
}
