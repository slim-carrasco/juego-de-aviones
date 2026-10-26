using System.Net.Http.Headers;
using System.Net.Http.Json;
using JuegoAviones.Contracts.DTO;
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
    private readonly SesionResponse sesion;
    private readonly Uri servidor;
    private readonly FlightCanvas canvas;
    private readonly ToolStripStatusLabel recordLabel = new("Elige un modo de juego");
    private readonly List<NetworkForm> ventanasPrueba = [];
    private bool abriendoPrueba;
    private int round;

    public Form1(SesionResponse sesion, Uri servidor)
    {
        this.sesion = sesion;
        this.servidor = servidor;
        api.BaseAddress = servidor;
        InitializeComponent();
        UiTheme.StyleTitleBar(this);
        Text = "Juego de aviones - Combate aéreo";
        ClientSize = new Size(900, 730);
        MinimumSize = new Size(850, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UiTheme.Background;
        KeyPreview = true;
        canvas = new FlightCanvas(game) { Dock = DockStyle.Fill };

        var barra = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(5),
            BackColor = UiTheme.Surface
        };
        var cuenta = new Label
        {
            Text = $"Sesión: {sesion.Nombre}",
            Width = 150,
            Height = 30,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = UiTheme.Text,
            Margin = new Padding(8, 3, 8, 3)
        };
        var solo = new Button { Text = "Solo", Width = 80 };
        var local = new Button { Text = "Duelo local (2)", Width = 135 };
        var prueba = new Button { Text = "3 vs 3 · esta PC", Width = 165 };
        var red = new Button { Text = "3 vs 3 · otras laptops", Width = 190 };
        UiTheme.StyleButton(solo);
        UiTheme.StyleButton(local, accent: true);
        UiTheme.StyleButton(prueba, accent: true);
        UiTheme.StyleButton(red, accent: true);
        solo.Click += (_, _) => StartRound(GameMode.Solo);
        local.Click += (_, _) => StartRound(GameMode.MultijugadorLocal);
        prueba.Click += async (_, _) => await AbrirPruebaLocalAsync();
        red.Click += (_, _) => { using var sala = new NetworkForm(sesion, servidor); sala.ShowDialog(this); };
        barra.Controls.AddRange([cuenta, solo, local, prueba, red]);

        var estado = new StatusStrip
        {
            BackColor = UiTheme.Surface,
            ForeColor = UiTheme.Text,
            SizingGrip = false
        };
        recordLabel.ForeColor = UiTheme.Text;
        estado.Items.Add(recordLabel);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        layout.Controls.Add(barra, 0, 0);
        layout.Controls.Add(canvas, 0, 1);
        layout.Controls.Add(estado, 0, 2);
        Controls.Add(layout);

        game.Completed += OnCompleted;
        KeyDown += (_, e) =>
        {
            bool firstPress = canvas.SetKey(e.KeyCode, true);
            if (firstPress && e.KeyCode is Keys.P or Keys.Escape) game.TogglePause();
            if (firstPress && e.KeyCode == Keys.R) StartRound(game.Mode);
            if (firstPress && e.KeyCode == Keys.Enter && game.State is FlightState.Ready or FlightState.Victory or FlightState.Defeat)
                StartRound(game.Mode);
            if (e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Space or Keys.Enter or
                Keys.W or Keys.A or Keys.S or Keys.D or Keys.F or Keys.P or Keys.Escape or Keys.R)
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

    private void StartRound(GameMode mode)
    {
        round++;
        recordLabel.Text = mode == GameMode.MultijugadorLocal ? "Modo multijugador local en curso" : "Partida individual en curso";
        canvas.ClearKeys();
        game.Start(mode);
        canvas.Focus();
    }

    private async Task AbrirPruebaLocalAsync()
    {
        if (abriendoPrueba) return;
        if (ventanasPrueba.Count > 0)
        {
            ventanasPrueba[0].Activate();
            recordLabel.Text = "La prueba de seis ventanas ya está abierta.";
            return;
        }
        abriendoPrueba = true;
        recordLabel.Text = "Preparando las seis cuentas de prueba…";
        try
        {
            var accesos = await Task.WhenAll(Enumerable.Range(1, 6).Select(async numero =>
            {
                string nombre = $"prueba{numero}";
                using var respuesta = await api.PostAsJsonAsync("/api/usuarios/login",
                    new CredencialesRequest(nombre, nombre), lifetime.Token);
                respuesta.EnsureSuccessStatusCode();
                return await respuesta.Content.ReadFromJsonAsync<SesionResponse>(cancellationToken: lifetime.Token)
                    ?? throw new InvalidOperationException($"Sin sesión para {nombre}.");
            }));
            var area = Screen.FromControl(this).WorkingArea;
            int ancho = area.Width / 3;
            int alto = area.Height / 2;
            foreach (var (acceso, indice) in accesos.Select((valor, indice) => (valor, indice)))
            {
                var ventana = new NetworkForm(acceso, servidor, compacta: true,
                    equipoPrueba: indice < 3 ? 1 : 2)
                {
                    Bounds = new Rectangle(area.Left + indice % 3 * ancho,
                        area.Top + indice / 3 * alto, ancho, alto)
                };
                ventana.FormClosed += (_, _) => ventanasPrueba.Remove(ventana);
                ventanasPrueba.Add(ventana);
                ventana.Show(this);
            }
            await Task.WhenAll(ventanasPrueba.Select(ventana => ventana.UnirPruebaLocalAsync()));
            recordLabel.Text = "Prueba local 3 vs 3: seis ventanas conectadas.";
        }
        catch (Exception error) when (error is HttpRequestException or OperationCanceledException or InvalidOperationException)
        {
            recordLabel.Text = "No se pudieron abrir las seis cuentas; revisa el servidor.";
            foreach (var ventana in ventanasPrueba.ToArray()) ventana.Close();
        }
        finally { abriendoPrueba = false; }
    }

    private async void OnCompleted()
    {
        int completedRound = round;
        recordLabel.Text = "Guardando resultado…";
        string status;
        try
        {
            bool duelo = game.Mode == GameMode.MultijugadorLocal;
            using var mensaje = new HttpRequestMessage(HttpMethod.Post, duelo ? "/api/partidas/duelo" : "/api/partidas")
            {
                Content = duelo
                    ? JsonContent.Create(new RegistrarDueloRequest
                    {
                        Ganador = game.State == FlightState.Victory ? 1 : 2,
                        VidaJugador1 = game.PlayerHealth,
                        VidaJugador2 = game.EnemyHealth
                    })
                    : JsonContent.Create(new RegistrarPartidaRequest
                    {
                        Jugador = sesion.Nombre,
                        Resultado = game.State == FlightState.Victory ? "Ganaste" : "Perdiste",
                        VidaJugador = game.PlayerHealth,
                        VidaRival = game.EnemyHealth
                    })
            };
            mensaje.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Token);
            using var respuesta = await api.SendAsync(mensaje, lifetime.Token);
            status = respuesta.IsSuccessStatusCode ? "Resultado guardado en MySQL ✓" :
                $"No se pudo guardar: {(int)respuesta.StatusCode}";
        }
        catch (HttpRequestException) { status = "Servidor sin conexión · resultado no guardado"; }
        catch (OperationCanceledException) { status = "Servidor · tiempo de espera agotado"; }
        if (!IsDisposed && completedRound == round) recordLabel.Text = status;
    }
}
