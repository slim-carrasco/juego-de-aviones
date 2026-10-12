using System.Net.Http.Json;
using JuegoAviones.Core.Gameplay;
using JuegoAviones.Render;

namespace JuegoAviones;

public partial class Form1 : Form
{
    private readonly FlightGame game = new();
    private readonly HttpClient api = new()
    {
        BaseAddress = new Uri("http://localhost:5080"),
        Timeout = TimeSpan.FromSeconds(5)
    };
    private readonly CancellationTokenSource lifetime = new();
    private readonly FlightCanvas canvas;
    private readonly ToolStripStatusLabel recordLabel = new("Presiona Enter para empezar");
    private int round;

    public Form1()
    {
        InitializeComponent();
        Text = "Juego de aviones - Combate aéreo";
        ClientSize = new Size(560, 780);
        MinimumSize = new Size(500, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(14, 23, 36);
        KeyPreview = true;
        canvas = new FlightCanvas(game) { Dock = DockStyle.Fill };
        Controls.Add(canvas);
        var statusBar = new StatusStrip();
        statusBar.Items.Add(recordLabel);
        Controls.Add(statusBar);
        game.Completed += OnCompleted;
        KeyDown += (_, e) =>
        {
            bool firstPress = canvas.SetKey(e.KeyCode, true);
            if (firstPress && e.KeyCode is Keys.P or Keys.Escape) game.TogglePause();
            if (firstPress && e.KeyCode == Keys.R) StartRound();
            if (firstPress && e.KeyCode == Keys.Enter && game.State is FlightState.Ready or FlightState.Victory or FlightState.Defeat)
                StartRound();
            if (e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down or
                Keys.Space or Keys.Enter or Keys.P or Keys.Escape or Keys.R)
                e.Handled = e.SuppressKeyPress = true;
        };
        KeyUp += (_, e) => { canvas.SetKey(e.KeyCode, false); e.Handled = true; };
        Deactivate += (_, _) =>
        {
            canvas.ClearKeys();
            if (game.State == FlightState.Playing) game.TogglePause();
        };
        FormClosed += (_, _) => { lifetime.Cancel(); api.Dispose(); lifetime.Dispose(); };
    }

    private void StartRound()
    {
        round++;
        recordLabel.Text = "Partida en curso";
        game.Start();
        canvas.Focus();
    }

    private async void OnCompleted()
    {
        int completedRound = round;
        recordLabel.Text = "Guardando resultado…";
        string status;
        try
        {
            using var response = await api.PostAsJsonAsync("/api/partidas", new
            {
                jugador = "Jugador",
                resultado = game.State == FlightState.Victory ? "Ganaste" : "Perdiste",
                vidaJugador = game.PlayerHealth,
                vidaRival = game.EnemyHealth
            }, lifetime.Token);
            status = response.IsSuccessStatusCode ? "Resultado guardado ✓" : "Servidor · no se pudo guardar";
        }
        catch (HttpRequestException) { status = "Servidor sin conexión · resultado no guardado"; }
        catch (OperationCanceledException) { status = "Servidor · tiempo de espera agotado"; }
        if (!IsDisposed && completedRound == round) recordLabel.Text = status;
    }
}
