using System.ComponentModel.DataAnnotations;

namespace JuegoAviones.Contracts.DTO;

public sealed class RegistrarPartidaRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Jugador { get; init; } = string.Empty;

    [Required, RegularExpression("^(Ganaste|Perdiste)$")]
    public string Resultado { get; init; } = string.Empty;

    [Range(0, 20)]
    public int VidaJugador { get; init; }

    [Range(0, 50)]
    public int VidaRival { get; init; }
}
