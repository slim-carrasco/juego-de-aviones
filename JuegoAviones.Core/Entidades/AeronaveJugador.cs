namespace JuegoAviones.Core.Entidades;

public sealed class AeronaveJugador : Aeronave
{
    public AeronaveJugador() : base("Jugador", new(280, 540), new(46, 68), 20, false) { }

    public override int AnguloRotacion => 0;

    // Polimorfismo: el jugador dispara desde la nariz hacia arriba.
    public override Projectile CrearDisparo() => new(Posicion.X, Posicion.Y - 42, false);
}
