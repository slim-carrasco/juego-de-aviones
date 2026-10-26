using JuegoAviones.Core.Gameplay;

namespace JuegoAviones.Tests;

public class TeamBattleTests
{
    private static TeamBattle CrearBatalla(TipoAvion tipo = TipoAvion.Ligero) =>
        new(Enumerable.Range(0, 6)
            .Select(i => ($"prueba{i + 1}", i < 3 ? Equipo.Azul : Equipo.Rojo, tipo))
            .ToList());

    [Fact]
    public void RequiereSeisParticipantesConTresPorEquipo()
    {
        var incompleto = Enumerable.Range(0, 5)
            .Select(i => ($"prueba{i + 1}", i < 3 ? Equipo.Azul : Equipo.Rojo, TipoAvion.Ligero))
            .ToList();
        Assert.Throws<ArgumentException>(() => new TeamBattle(incompleto));
    }

    [Fact]
    public void ElServidorMueveYLimitaLosAviones()
    {
        var batalla = CrearBatalla();
        float inicio = batalla.Pilotos[0].X;
        batalla.RecibirEntrada(0, new EntradaBatalla(-1, 0, false));
        batalla.Actualizar(1f / 30);
        Assert.True(batalla.Pilotos[0].X < inicio);
        for (int i = 0; i < 200; i++) batalla.Actualizar(1f / 30);
        Assert.True(batalla.Pilotos[0].Limites.Left >= 0);
        Assert.Equal(EstadoBatalla.Jugando, batalla.Estado);
    }

    [Fact]
    public void TresDerribosTerminanElCombateSinInputsDelRival()
    {
        var batalla = CrearBatalla();
        for (int i = 0; i < 3; i++)
            batalla.RecibirEntrada(i, new EntradaBatalla(0, 0, true));
        for (int i = 0; i < 300 && batalla.Estado == EstadoBatalla.Jugando; i++)
            batalla.Actualizar(1f / 30);
        Assert.Equal(EstadoBatalla.Terminada, batalla.Estado);
        Assert.Equal(Equipo.Azul, batalla.Ganador);
        Assert.All(batalla.Pilotos.Where(p => p.Equipo == Equipo.Rojo), p => Assert.Equal(0, p.Vida));
        Assert.Equal(3, batalla.Pilotos.Sum(p => p.Derribos));
    }

    [Fact]
    public void DesconexionEliminaAlPilotoYElEquipoContinuaHastaPerderLosTres()
    {
        var batalla = CrearBatalla(TipoAvion.Pesado);
        Assert.All(batalla.Pilotos, p => Assert.Equal(30, p.Vida));
        batalla.Desconectar(3);
        Assert.Equal(EstadoBatalla.Jugando, batalla.Estado);
        batalla.Desconectar(4);
        batalla.Desconectar(5);
        Assert.Equal(EstadoBatalla.Terminada, batalla.Estado);
        Assert.Equal(Equipo.Azul, batalla.Ganador);
    }
}
