using JuegoAviones.Core.Gameplay;

namespace JuegoAviones.Tests;

public class FlightGameTests
{
    [Fact]
    public void ReadyAndPaused_DoNotAdvanceOrFire()
    {
        var game = new FlightGame();
        game.Update(.05f, 1, 1, true);
        Assert.Equal(0, game.Elapsed);
        game.Start();
        game.TogglePause();
        var player = game.Player;
        var enemy = game.Enemy;
        game.Update(.05f, -1, -1, true);
        Assert.Equal(player, game.Player);
        Assert.Equal(enemy, game.Enemy);
        Assert.Empty(game.Projectiles);
        game.TogglePause();
        game.Update(.05f, 1, 0, true);
        Assert.True(game.Player.X > player.X);
        Assert.Single(game.Projectiles);
    }

    [Fact]
    public void Movement_StaysInsideArenaAndNormalizesDiagonalSpeed()
    {
        var straight = new FlightGame();
        var diagonal = new FlightGame();
        straight.Start();
        diagonal.Start();
        straight.Update(.05f, 1, 0, false);
        diagonal.Update(.05f, 1, -1, false);
        float distance = MathF.Sqrt(MathF.Pow(diagonal.Player.X - 280, 2) + MathF.Pow(diagonal.Player.Y - 540, 2));
        Assert.Equal(straight.Player.X - 280, distance, precision: 3);
        for (int i = 0; i < 200; i++) diagonal.Update(.05f, -1, -1, false);
        Assert.True(diagonal.PlayerBounds.Left >= 0);
        Assert.True(diagonal.PlayerBounds.Top >= 0);
        for (int i = 0; i < 200; i++) diagonal.Update(.05f, 1, 1, false);
        Assert.True(diagonal.PlayerBounds.Right <= FlightGame.Width);
        Assert.True(diagonal.PlayerBounds.Bottom <= FlightGame.Height);
    }

    [Fact]
    public void HoldingFire_RespectsCooldownAndRemovesOffscreenProjectiles()
    {
        var game = new FlightGame();
        game.Start();
        for (int i = 0; i < 20; i++) game.Update(.05f, 0, 0, true);
        Assert.InRange(game.Shots, 4, 5);
        for (int i = 0; i < 60; i++) game.Update(.05f, 0, 0, false);
        Assert.DoesNotContain(game.Projectiles, p => !p.Enemy);
        Assert.All(game.Projectiles, p => Assert.InRange(p.Y, -20, FlightGame.Height + 20));
    }

    [Fact]
    public void Defeat_ReportsCompletionOnceAndRestartRestoresRound()
    {
        var game = new FlightGame();
        int completed = 0;
        game.Completed += () => completed++;
        game.Start();
        for (int i = 0; i < 3000 && game.State == FlightState.Playing; i++)
            game.Update(.05f, 0, 0, false);
        Assert.Equal(FlightState.Defeat, game.State);
        Assert.Equal(0, game.PlayerHealth);
        Assert.Equal(1, completed);
        game.Update(.05f, 1, 1, true);
        Assert.Equal(1, completed);
        game.Start();
        Assert.Equal(FlightState.Playing, game.State);
        Assert.Equal(20, game.PlayerHealth);
        Assert.Equal(50, game.EnemyHealth);
        Assert.Equal(0, game.Elapsed);
        Assert.Equal(0, game.Shots);
        Assert.Empty(game.Projectiles);
    }

    [Fact]
    public void AimedFire_CanWinWithFiveHits()
    {
        var game = new FlightGame();
        game.Start();
        float previousEnemyX = game.Enemy.X - 1;
        for (int i = 0; i < 3000 && game.State == FlightState.Playing; i++)
        {
            // Lead the moving target by the projectile's travel time.
            float direction = game.Enemy.X >= previousEnemyX ? 1 : -1;
            float travelTime = (game.Player.Y - 42 - game.EnemyBounds.Bottom) / 500;
            float targetX = game.Enemy.X + direction * 95 * travelTime;
            if (targetX > FlightGame.Width - 44) targetX = 2 * (FlightGame.Width - 44) - targetX;
            if (targetX < 44) targetX = 88 - targetX;
            previousEnemyX = game.Enemy.X;
            float horizontal = Math.Clamp((targetX - game.Player.X) / 13.5f, -1, 1);
            game.Update(.05f, horizontal, 0, true);
        }
        Assert.Equal(FlightState.Victory, game.State);
        Assert.Equal(5, game.Hits);
        Assert.Equal(0, game.EnemyHealth);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(-1f)]
    public void InvalidTime_DoesNotCorruptPositions(float seconds)
    {
        var game = new FlightGame();
        game.Start();
        game.Update(seconds, 1, 1, true);
        Assert.Equal(0, game.Elapsed);
        Assert.Equal(280, game.Player.X);
    }

    [Fact]
    public void MultijugadorLocal_ControlaDosAvionesSinDisparosAutomaticos()
    {
        var game = new FlightGame();
        game.Start(GameMode.MultijugadorLocal);
        Assert.Equal(20, game.EnemyHealth);
        game.Update(.05f, -1, 0, false, 1, 0, true);
        Assert.True(game.Player.X < 280);
        Assert.True(game.Enemy.X > 280);
        Assert.Single(game.Projectiles);
        Assert.True(game.Projectiles[0].Enemy);

        for (int i = 0; i < 40; i++) game.Update(.05f, 0, 0, false, 0, 0, false);
        Assert.Empty(game.Projectiles);
        Assert.Equal(FlightState.Playing, game.State);
    }

    [Fact]
    public void MultijugadorLocal_DosImpactosDecidenGanadorYReinicioConservaModo()
    {
        var game = new FlightGame();
        game.Start(GameMode.MultijugadorLocal);
        for (int i = 0; i < 100 && game.State == FlightState.Playing; i++)
            game.Update(.05f, 0, 0, true, 0, 0, false);
        Assert.Equal(FlightState.Victory, game.State);
        Assert.Equal(0, game.EnemyHealth);
        Assert.Equal(2, game.Hits);

        game.Start(game.Mode);
        Assert.Equal(GameMode.MultijugadorLocal, game.Mode);
        Assert.Equal(20, game.EnemyHealth);
    }
}
