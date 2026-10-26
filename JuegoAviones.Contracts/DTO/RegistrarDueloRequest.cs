using System.ComponentModel.DataAnnotations;

namespace JuegoAviones.Contracts.DTO;

public sealed class RegistrarDueloRequest
{
    [Range(1, 2)]
    public int Ganador { get; init; }

    [Range(0, 20)]
    public int VidaJugador1 { get; init; }

    [Range(0, 20)]
    public int VidaJugador2 { get; init; }
}
