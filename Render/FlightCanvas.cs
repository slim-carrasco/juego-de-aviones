using System.Diagnostics;
using System.Drawing.Drawing2D;
using JuegoAviones.Core.Entidades;
using JuegoAviones.Core.Gameplay;

namespace JuegoAviones.Render;

// Este control dibuja el escenario, los aviones y los disparos.
public sealed class FlightCanvas : Control
{
    private readonly FlightGame game;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly HashSet<Keys> keys = [];
    private readonly Font titleFont = new("Segoe UI", 18, FontStyle.Bold, GraphicsUnit.Pixel);
    private readonly Font textFont = new("Segoe UI", 13, FontStyle.Regular, GraphicsUnit.Pixel);
    private readonly Image aircraftImage;
    private double previousTime;
    private double updateTotalMilliseconds;
    private double updateAverageMilliseconds;
    private int updateSamples;
    private bool hasProfile;
    private float scale = 1;
    private float offsetX;
    private float offsetY;

    public FlightCanvas(FlightGame game)
    {
        this.game = game;
        aircraftImage = Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "condor-fighter.png"));
        DoubleBuffered = true; // Evita que el dibujo parpadee al moverse.
        SetStyle(ControlStyles.ResizeRedraw, true);
        timer.Tick += (_, _) =>
        {
            double now = clock.Elapsed.TotalSeconds;
            float seconds = (float)(now - previousTime);
            previousTime = now;
            float horizontal = (keys.Contains(Keys.Right) ? 1 : 0) - (keys.Contains(Keys.Left) ? 1 : 0);
            float vertical = (keys.Contains(Keys.Down) ? 1 : 0) - (keys.Contains(Keys.Up) ? 1 : 0);
            bool fire = keys.Contains(Keys.Space) || (game.Mode == GameMode.Solo && keys.Contains(Keys.Enter));
            float rivalHorizontal = (keys.Contains(Keys.D) ? 1 : 0) - (keys.Contains(Keys.A) ? 1 : 0);
            float rivalVertical = (keys.Contains(Keys.S) ? 1 : 0) - (keys.Contains(Keys.W) ? 1 : 0);
            bool rivalFire = keys.Contains(Keys.F);
            // Profiling: medir solo el tiempo de la lógica durante la partida.
            bool measuring = game.State == FlightState.Playing;
            long started = Stopwatch.GetTimestamp();
            game.Update(seconds, horizontal, vertical, fire, rivalHorizontal, rivalVertical, rivalFire);
            if (measuring)
            {
                updateTotalMilliseconds += Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                updateSamples++;
                if (updateSamples == 60)
                {
                    updateAverageMilliseconds = updateTotalMilliseconds / updateSamples;
                    hasProfile = true;
                    updateSamples = 0;
                    updateTotalMilliseconds = 0;
                }
            }
            Invalidate(); // Solicita volver a dibujar la pantalla.
        };
        previousTime = clock.Elapsed.TotalSeconds;
        timer.Start();
    }

    public bool SetKey(Keys key, bool pressed) => pressed ? keys.Add(key) : keys.Remove(key);
    public void ClearKeys() => keys.Clear();
    protected override bool IsInputKey(Keys keyData) => true;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.Clear(Color.FromArgb(14, 23, 36));
        // El escenario conserva sus proporciones aunque cambie el tamaño de la ventana.
        scale = Math.Max(.1f, Math.Min(ClientSize.Width / 560f, ClientSize.Height / 750f));
        offsetX = (ClientSize.Width - 560 * scale) / 2;
        offsetY = (ClientSize.Height - 750 * scale) / 2;
        g.TranslateTransform(offsetX, offsetY);
        g.ScaleTransform(scale, scale);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.DrawString($"J1: {game.PlayerHealth} / 20", titleFont, Brushes.Turquoise, 16, 12);
        g.DrawString($"{(game.Mode == GameMode.MultijugadorLocal ? "J2" : "Rival")}: {game.EnemyHealth} / {game.EnemyMaxHealth}",
            titleFont, Brushes.Coral, 350, 12);
        g.TranslateTransform(0, 48);
        using (var sky = new LinearGradientBrush(new Rectangle(0, 0, 560, 640),
            Color.FromArgb(26, 53, 74), Color.FromArgb(12, 28, 45), 90f))
            g.FillRectangle(sky, 0, 0, FlightGame.Width, FlightGame.Height);

        // Nubes hechas con óvalos: son solo decoración del escenario.
        DrawCloud(g, 40, 80);
        DrawCloud(g, 385, 235);
        DrawCloud(g, 90, 390);
        if (game.State != FlightState.Defeat) DrawPlane(g, game.AvionJugador);
        if (game.State != FlightState.Victory) DrawPlane(g, game.AvionRival);
        foreach (var shot in game.Projectiles)
            g.FillRectangle(shot.Enemy ? Brushes.Coral : Brushes.Turquoise, shot.X - 3, shot.Y - 8, 6, 16);

        if (game.State != FlightState.Playing)
        {
            string message = game.State switch
            {
                FlightState.Ready => "JUEGO DE AVIONES",
                FlightState.Paused => "JUEGO EN PAUSA",
                FlightState.Victory => game.Mode == GameMode.MultijugadorLocal ? "¡GANA J1!" : "¡GANASTE!",
                _ => game.Mode == GameMode.MultijugadorLocal ? "¡GANA J2!" : "PERDISTE"
            };
            string instruction = game.State == FlightState.Paused ? "Presiona P para continuar" :
                game.State == FlightState.Ready ? "Presiona Enter para empezar" : "Presiona R para volver a jugar";
            bool ready = game.State == FlightState.Ready;
            using var panel = new SolidBrush(Color.FromArgb(235, 14, 23, 36));
            g.FillRectangle(panel, 80, ready ? 160 : 265, 400, ready ? 330 : 100);
            using var centered = new StringFormat { Alignment = StringAlignment.Center };
            g.DrawString(message, titleFont, Brushes.WhiteSmoke, new RectangleF(80, ready ? 178 : 286, 400, 28), centered);
            if (ready) g.DrawImage(aircraftImage, 205, 216, 150, 225);
            g.DrawString(instruction, textFont, Brushes.Turquoise, new RectangleF(80, ready ? 456 : 326, 400, 24), centered);
        }
        g.TranslateTransform(0, -48);
        string profile = hasProfile ? $"Profiling: lógica {updateAverageMilliseconds:F4} ms (promedio de 60 actualizaciones)" :
            "Profiling: inicia la partida para medir el tiempo de la lógica";
        g.DrawString(profile, textFont, Brushes.LightGray, 12, 693);
        string controls = game.Mode == GameMode.MultijugadorLocal
            ? "J1: flechas + Espacio  |  J2: WASD + F  |  P: pausa  |  R: reiniciar"
            : "Flechas: mover  |  Enter / Espacio: disparar  |  P: pausa  |  R: reiniciar";
        g.DrawString(controls, textFont, Brushes.LightGray, 12, 722);
    }

    // El cliente dibuja la imagen usando la posición del objeto Aeronave.
    private void DrawPlane(Graphics g, Aeronave avion)
    {
        var saved = g.Save();
        g.TranslateTransform(avion.Posicion.X, avion.Posicion.Y);
        g.RotateTransform(avion.AnguloRotacion);
        float width = avion.EsRival ? 78 : 64;
        float height = avion.EsRival ? 112 : 96;
        g.DrawImage(aircraftImage, -width / 2, -height / 2, width, height);
        g.Restore(saved);
        g.DrawRectangle(avion.EsRival ? Pens.Coral : Pens.Turquoise,
            avion.Posicion.X - 7, avion.Posicion.Y + height / 2 + 4, 14, 3);
    }

    private static void DrawCloud(Graphics g, float x, float y)
    {
        using var cloud = new SolidBrush(Color.FromArgb(25, Color.White));
        g.FillEllipse(cloud, x, y + 14, 80, 28);
        g.FillEllipse(cloud, x + 16, y, 42, 38);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Dispose();
            aircraftImage.Dispose();
            titleFont.Dispose();
            textFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
