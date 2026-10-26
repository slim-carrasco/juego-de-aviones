using JuegoAviones.Core.Entidades;

namespace JuegoAviones.Core.Interfaces;

public interface IResultadoEquipoRepository
{
    Task<int> GuardarAsync(PartidaEquipo partida, CancellationToken cancellationToken);
}
