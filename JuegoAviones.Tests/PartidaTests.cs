using JuegoAviones.Core.Entidades;

namespace JuegoAviones.Tests;

public class PartidaTests
{
    [Fact]
    public void RegistrarResultado_CreaPartidaValida()
    {
        var partida = Partida.RegistrarResultado(
            " Ana ",
            ResultadoPartida.Ganaste,
            vidaJugador: 20,
            vidaRival: 0,
            fechaUtc: DateTime.UnixEpoch);

        Assert.Equal("Ana", partida.Jugador);
        Assert.Equal(ResultadoPartida.Ganaste, partida.Resultado);
        Assert.Equal(DateTime.UnixEpoch, partida.FechaUtc);
    }

    [Theory]
    [InlineData(21, 0)]
    [InlineData(20, 51)]
    [InlineData(-1, 0)]
    public void RegistrarResultado_RechazaValoresDeVidaInvalidos(int vidaJugador, int vidaRival)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Partida.RegistrarResultado(
            "Ana",
            ResultadoPartida.Ganaste,
            vidaJugador,
            vidaRival));
    }

    [Fact]
    public void RegistrarResultado_RechazaJugadorVacio()
    {
        Assert.Throws<ArgumentException>(() => Partida.RegistrarResultado(
            " ",
            ResultadoPartida.Perdiste,
            0,
            50));
    }
}
