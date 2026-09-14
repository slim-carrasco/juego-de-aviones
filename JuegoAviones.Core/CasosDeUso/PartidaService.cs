using JuegoAviones.Core.Entidades;
using JuegoAviones.Core.Interfaces;

namespace JuegoAviones.Core.CasosDeUso;

public sealed class PartidaService(IPartidaRepository partidas)
{
    public async Task<Partida> RegistrarAsync(
        string jugador,
        ResultadoPartida resultado,
        int vidaJugador,
        int vidaRival,
        CancellationToken cancellationToken)
    {
        var partida = Partida.RegistrarResultado(jugador, resultado, vidaJugador, vidaRival);
        await partidas.AgregarAsync(partida, cancellationToken);
        return partida;
    }

    public Task<Partida?> ObtenerAsync(int id, CancellationToken cancellationToken) =>
        partidas.ObtenerPorIdAsync(id, cancellationToken);

    public async Task<IReadOnlyList<(string Jugador, int Partidas, int Victorias, int Derrotas)>> RankingAsync(
        CancellationToken cancellationToken)
    {
        var registros = await partidas.ObtenerTodasAsync(cancellationToken);

        return registros
            .GroupBy(partida => partida.Jugador)
            .Select(grupo => (
                Jugador: grupo.Key,
                Partidas: grupo.Count(),
                Victorias: grupo.Count(partida => partida.Resultado == ResultadoPartida.Ganaste),
                Derrotas: grupo.Count(partida => partida.Resultado == ResultadoPartida.Perdiste)))
            .OrderByDescending(resultado => resultado.Victorias)
            .ThenByDescending(resultado => resultado.Partidas)
            .ThenBy(resultado => resultado.Jugador)
            .Take(10)
            .ToList();
    }
}
