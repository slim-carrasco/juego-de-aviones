using JuegoAviones.Core.Gameplay;

namespace JuegoAviones.Core.Entidades;

public sealed class PartidaEquipo
{
    private PartidaEquipo() { }

    public int Id { get; private set; }
    public Equipo EquipoGanador { get; private set; }
    public float DuracionSegundos { get; private set; }
    public DateTime FechaUtc { get; private set; }
    public List<DetalleJugadorPartida> Detalles { get; private set; } = [];

    public static PartidaEquipo DesdeBatalla(TeamBattle batalla)
    {
        if (batalla.Estado != EstadoBatalla.Terminada || batalla.Ganador is null)
            throw new ArgumentException("La batalla debe haber terminado.", nameof(batalla));
        return new PartidaEquipo
        {
            EquipoGanador = batalla.Ganador.Value,
            DuracionSegundos = batalla.DuracionSegundos,
            FechaUtc = DateTime.UtcNow,
            Detalles = batalla.Pilotos.Select(p => new DetalleJugadorPartida
            {
                Usuario = p.Usuario,
                Equipo = p.Equipo,
                Avion = p.Avion,
                Derribos = p.Derribos,
                Disparos = p.Disparos,
                Aciertos = p.Aciertos,
                VidaFinal = p.Vida,
                ConectadoAlFinal = p.Conectado
            }).ToList()
        };
    }
}

public sealed class DetalleJugadorPartida
{
    public int Id { get; private set; }
    public int PartidaEquipoId { get; private set; }
    public string Usuario { get; internal set; } = string.Empty;
    public Equipo Equipo { get; internal set; }
    public TipoAvion Avion { get; internal set; }
    public int Derribos { get; internal set; }
    public int Disparos { get; internal set; }
    public int Aciertos { get; internal set; }
    public int VidaFinal { get; internal set; }
    public bool ConectadoAlFinal { get; internal set; }
}
