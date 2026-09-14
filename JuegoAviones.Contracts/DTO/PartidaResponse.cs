namespace JuegoAviones.Contracts.DTO;

public sealed record PartidaResponse(
    int Id,
    string Jugador,
    string Resultado,
    int VidaJugador,
    int VidaRival,
    DateTime FechaUtc);
