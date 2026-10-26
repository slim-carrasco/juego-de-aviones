using System.Drawing;
using JuegoAviones.Core.Entidades;

namespace JuegoAviones.Core.Gameplay;

public enum FlightState { Ready, Playing, Paused, Victory, Defeat }
public enum GameMode { Solo, MultijugadorLocal }

// Reglas del juego. Las posiciones se miden dentro de un escenario de 560 x 640.
public sealed class FlightGame
{
    public const float Width = 560;
    public const float Height = 640;
    private readonly List<Projectile> projectiles = [];
    private float enemyDirection = 1;
    private float enemyAutoFire;
    private float rivalFire;
    private float playerFire;
    public GameMode Mode { get; private set; } = GameMode.Solo;
    public FlightState State { get; private set; } = FlightState.Ready;
    public Aeronave AvionJugador { get; private set; } = CrearJugador();
    public Aeronave AvionRival { get; private set; } = CrearRival();
    public PointF Player => AvionJugador.Posicion;
    public PointF Enemy => AvionRival.Posicion;
    public int PlayerHealth => AvionJugador.Vida;
    public int EnemyHealth => AvionRival.Vida;
    public int EnemyMaxHealth => AvionRival.VidaInicial;
    public int Shots { get; private set; }
    public int Hits { get; private set; }
    public float Elapsed { get; private set; }
    public float HitFlash { get; private set; }
    public IReadOnlyList<Projectile> Projectiles => projectiles;
    public RectangleF PlayerBounds => AvionJugador.Limites;
    public RectangleF EnemyBounds => AvionRival.Limites;
    public event Action? Completed;

    private static Aeronave CrearJugador() => new AeronaveJugador();
    private static Aeronave CrearRival(GameMode mode = GameMode.Solo) =>
        new AeronaveRival(mode == GameMode.MultijugadorLocal);

    public void Start(GameMode mode = GameMode.Solo)
    {
        Mode = mode;
        // Reiniciar posiciones, vidas y proyectiles para una nueva partida.
        projectiles.Clear();
        AvionJugador = CrearJugador();
        AvionRival = CrearRival(mode);
        Shots = Hits = 0;
        Elapsed = HitFlash = playerFire = rivalFire = enemyAutoFire = 0;
        enemyDirection = 1;
        State = FlightState.Playing;
    }

    public void TogglePause()
    {
        if (State == FlightState.Playing) State = FlightState.Paused;
        else if (State == FlightState.Paused) State = FlightState.Playing;
    }

    public void Update(float seconds, float horizontal, float vertical, bool firing)
        => Update(seconds, horizontal, vertical, firing, 0, 0, false);

    public void Update(float seconds, float horizontal, float vertical, bool firing,
        float rivalHorizontal, float rivalVertical, bool rivalFiring)
    {
        if (State != FlightState.Playing || seconds <= 0 || !float.IsFinite(seconds)) return;
        float dt = Math.Min(seconds, .05f);
        Elapsed += dt;
        HitFlash = Math.Max(0, HitFlash - dt);
        playerFire = Math.Max(0, playerFire - dt);
        rivalFire = Math.Max(0, rivalFire - dt);
        float magnitude = MathF.Sqrt(horizontal * horizontal + vertical * vertical);
        if (magnitude > 1) { horizontal /= magnitude; vertical /= magnitude; }
        // Math.Clamp impide que el jugador salga del escenario.
        AvionJugador.MoverA(
            Math.Clamp(Player.X + horizontal * 270 * dt, 34, Width - 34),
            Math.Clamp(Player.Y + vertical * 270 * dt, 190, Height - 48));
        if (Mode == GameMode.Solo)
        {
            // El rival automático se mueve de lado a lado.
            AvionRival.MoverA(Math.Clamp(Enemy.X + enemyDirection * 95 * dt, 44, Width - 44), Enemy.Y);
            if (Enemy.X <= 44 || Enemy.X >= Width - 44) enemyDirection *= -1;
        }
        else
        {
            float rivalMagnitude = MathF.Sqrt(rivalHorizontal * rivalHorizontal + rivalVertical * rivalVertical);
            if (rivalMagnitude > 1) { rivalHorizontal /= rivalMagnitude; rivalVertical /= rivalMagnitude; }
            AvionRival.MoverA(
                Math.Clamp(Enemy.X + rivalHorizontal * 270 * dt, 44, Width - 44),
                Math.Clamp(Enemy.Y + rivalVertical * 270 * dt, 48, 290));
        }
        if (firing && playerFire <= 0)
        {
            projectiles.Add(AvionJugador.CrearDisparo());
            Shots++;
            playerFire = .22f;
        }
        if (Mode == GameMode.Solo)
        {
            enemyAutoFire += dt;
            if (enemyAutoFire >= .9f)
            {
                projectiles.Add(AvionRival.CrearDisparo());
                enemyAutoFire -= .9f;
            }
        }
        else if (rivalFiring && rivalFire <= 0)
        {
            projectiles.Add(AvionRival.CrearDisparo());
            rivalFire = .22f;
        }
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            // Recorrer desde el final permite eliminar disparos sin saltar elementos.
            var shot = projectiles[i];
            shot.Y += (shot.Enemy ? 310 : -500) * dt;
            if (shot.Bounds.IntersectsWith(shot.Enemy ? PlayerBounds : EnemyBounds))
            {
                // Hay impacto cuando los rectángulos del disparo y del avión se cruzan.
                projectiles.RemoveAt(i);
                if (shot.Enemy) { AvionJugador.RecibirImpacto(10); HitFlash = .25f; }
                else { AvionRival.RecibirImpacto(10); Hits++; }
                if (PlayerHealth == 0 || EnemyHealth == 0)
                {
                    State = PlayerHealth == 0 ? FlightState.Defeat : FlightState.Victory;
                    projectiles.Clear();
                    Completed?.Invoke();
                    return;
                }
            }
            else if (shot.Y < -20 || shot.Y > Height + 20) projectiles.RemoveAt(i);
        }
    }
}
