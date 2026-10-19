namespace JuegoAviones.Core.Entidades;

public sealed class Partida
{
    private Partida() { }

    public int Id { get; private set; }
    public string Jugador { get; private set; } = string.Empty;
    public ResultadoPartida Resultado { get; private set; }
    public int VidaJugador { get; private set; }
    public int VidaRival { get; private set; }
    public DateTime FechaUtc { get; private set; }

    public static Partida RegistrarResultado(
        string jugador,
        ResultadoPartida resultado,
        int vidaJugador,
        int vidaRival,
        DateTime? fechaUtc = null)
    {
        if (string.IsNullOrWhiteSpace(jugador))
            throw new ArgumentException("El jugador es obligatorio.", nameof(jugador));

        if (jugador.Trim().Length > 50)
            throw new ArgumentException("El jugador no puede superar 50 caracteres.", nameof(jugador));

        if (vidaJugador is < 0 or > 20)
            throw new ArgumentOutOfRangeException(nameof(vidaJugador), "La vida del jugador debe estar entre 0 y 20.");

        if (vidaRival is < 0 or > 50)
            throw new ArgumentOutOfRangeException(nameof(vidaRival), "La vida del rival debe estar entre 0 y 50.");

        return new Partida
        {
            Jugador = jugador.Trim(),
            Resultado = resultado,
            VidaJugador = vidaJugador,
            VidaRival = vidaRival,
            FechaUtc = fechaUtc ?? DateTime.UtcNow
        };
    }
}

public enum ResultadoPartida
{
    Ganaste = 1,
    Perdiste = 2
}
