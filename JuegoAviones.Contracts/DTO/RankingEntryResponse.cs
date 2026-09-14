namespace JuegoAviones.Contracts.DTO;

public sealed record RankingEntryResponse(
    string Jugador,
    int Partidas,
    int Victorias,
    int Derrotas);
