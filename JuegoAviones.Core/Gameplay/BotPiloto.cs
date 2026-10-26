namespace JuegoAviones.Core.Gameplay;

// Los bots usan el mismo sistema de entradas que los jugadores humanos.
public static class BotPiloto
{
    public static EntradaBatalla Decidir(TeamBattle batalla, int indice)
    {
        if (indice < 0 || indice >= batalla.Pilotos.Count) return default;
        var piloto = batalla.Pilotos[indice];
        if (piloto.Vida <= 0) return default;

        var rival = batalla.Pilotos
            .Where(p => p.Equipo != piloto.Equipo && p.Vida > 0)
            .MinBy(p => MathF.Abs(p.X - piloto.X));
        if (rival is null) return default;

        int horizontal = MathF.Abs(rival.X - piloto.X) > 12
            ? Math.Sign(rival.X - piloto.X) : 0;
        return new EntradaBatalla(horizontal, 0, MathF.Abs(rival.X - piloto.X) < 28);
    }
}
