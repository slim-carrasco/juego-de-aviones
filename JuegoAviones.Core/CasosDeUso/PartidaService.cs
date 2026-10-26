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

    public async Task<(Partida Jugador1, Partida Jugador2)> RegistrarDueloAsync(
        string jugador1, string jugador2, int ganador, int vidaJugador1, int vidaJugador2,
        CancellationToken cancellationToken)
    {
        if (ganador is not (1 or 2) || vidaJugador1 is < 0 or > 20 || vidaJugador2 is < 0 or > 20 ||
            (ganador == 1 && (vidaJugador1 == 0 || vidaJugador2 != 0)) ||
            (ganador == 2 && (vidaJugador2 == 0 || vidaJugador1 != 0)))
            throw new ArgumentException("El ganador y las vidas no coinciden con el resultado del duelo.");

        var primero = Partida.RegistrarResultado(jugador1,
            ganador == 1 ? ResultadoPartida.Ganaste : ResultadoPartida.Perdiste,
            vidaJugador1, vidaJugador2, modo: "MultijugadorLocal", oponente: jugador2);
        var segundo = Partida.RegistrarResultado(jugador2,
            ganador == 2 ? ResultadoPartida.Ganaste : ResultadoPartida.Perdiste,
            vidaJugador2, vidaJugador1, modo: "MultijugadorLocal", oponente: jugador1);
        await partidas.AgregarMultiplesAsync([primero, segundo], cancellationToken);
        return (primero, segundo);
    }

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
