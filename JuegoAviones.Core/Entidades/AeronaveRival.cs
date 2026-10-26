namespace JuegoAviones.Core.Entidades;

public sealed class AeronaveRival : Aeronave
{
    public AeronaveRival(bool controlado = false)
        : base(controlado ? "Jugador 2" : "Rival", new(280, 92), new(60, 80), controlado ? 20 : 50, true) { }

    public override int AnguloRotacion => 180;

    // El mismo método tiene otro comportamiento para el rival: dispara hacia abajo.
    public override Projectile CrearDisparo() => new(Posicion.X, Posicion.Y + 46, true);
}
