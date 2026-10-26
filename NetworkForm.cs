using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using JuegoAviones.Contracts.DTO;
using JuegoAviones.Core.Gameplay;

namespace JuegoAviones;

public sealed class NetworkForm : Form
{
    private readonly SesionResponse sesion;
    private readonly Uri servidor;
    private readonly bool compacta;
    private readonly NetworkCanvas campo = new() { Dock = DockStyle.Fill, Mensaje = "ELIGE EQUIPO Y PULSA BUSCAR" };
    private readonly ComboBox avion = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 85 };
    private readonly ComboBox equipo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 145 };
    private readonly Label estado = new() { Dock = DockStyle.Fill, ForeColor = Color.White,
        Padding = new Padding(12, 5, 8, 3), TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button conectar = new() { Text = "Buscar partida", Width = 130 };
    private readonly CheckBox completarBots = new() { Text = "Completar con bots", AutoSize = true,
        ForeColor = Color.White, Padding = new Padding(0, 7, 0, 0) };
    private readonly HashSet<Keys> teclas = [];
    private readonly System.Windows.Forms.Timer controles = new() { Interval = 33 };
    private ClientWebSocket? socket;
    private readonly CancellationTokenSource vida = new();
    private bool jugando;
    private bool enviando;
    private bool botsSolicitados;
    private int ultimaVersionEspera = -1;

    public NetworkForm(SesionResponse sesion, Uri servidor, bool compacta = false, int equipoPrueba = 1)
    {
        this.sesion = sesion;
        this.servidor = servidor;
        this.compacta = compacta;
        Text = $"Aeronaves online · {sesion.Nombre}";
        ClientSize = compacta ? new Size(400, 340) : new Size(650, 760);
        MinimumSize = compacta ? new Size(300, 240) : new Size(620, 700);
        StartPosition = compacta ? FormStartPosition.Manual : FormStartPosition.CenterParent;
        KeyPreview = true;
        BackColor = UiTheme.Background;
        UiTheme.StyleTitleBar(this);

        avion.Items.AddRange(["Ligero", "Pesado"]);
        avion.SelectedIndex = 0;
        equipo.Items.AddRange(["Equipo 1 (Azul)", "Equipo 2 (Rojo)"]);
        equipo.SelectedIndex = compacta ? equipoPrueba - 1 : -1;
        estado.Text = compacta
            ? $"Conectando a {sesion.Nombre} · Equipo {equipoPrueba}…"
            : $"Sin conectar · Cuenta: {sesion.Nombre}\nElige equipo y pulsa Buscar partida.";
        UiTheme.StyleButton(conectar, accent: true);
        conectar.Click += async (_, _) => await ConectarAsync();
        completarBots.CheckedChanged += async (_, _) =>
        {
            if (completarBots.Checked) await SolicitarBotsAsync();
        };
        var barra = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface,
            Padding = new Padding(8), WrapContents = true };
        foreach (var control in new Control[]
        {
            Etiqueta("Avión"), avion, Etiqueta("Tu equipo"), equipo, conectar, completarBots
        }) barra.Controls.Add(control);
        var cabecera = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
        cabecera.Controls.Add(estado);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, compacta ? 0 : 84));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(barra, 0, 0);
        layout.Controls.Add(cabecera, 0, 1);
        layout.Controls.Add(campo, 0, 2);
        if (compacta) barra.Visible = false;
        Controls.Add(layout);

        KeyDown += (_, e) => { teclas.Add(e.KeyCode); if (EsControl(e.KeyCode)) e.SuppressKeyPress = true; };
        KeyUp += (_, e) => { teclas.Remove(e.KeyCode); if (EsControl(e.KeyCode)) e.SuppressKeyPress = true; };
        Deactivate += (_, _) => teclas.Clear();
        controles.Tick += async (_, _) => await EnviarControlesAsync();
        FormClosed += (_, _) => { controles.Stop(); vida.Cancel(); socket?.Dispose(); vida.Dispose(); campo.Dispose(); };
    }

    private static Label Etiqueta(string texto) => new()
    {
        Text = texto, AutoSize = true, ForeColor = UiTheme.Text,
        TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(0, 7, 0, 0)
    };

    private static bool EsControl(Keys key) => key is Keys.Left or Keys.Right or Keys.Up or Keys.Down or
        Keys.W or Keys.A or Keys.S or Keys.D or Keys.Space;

    public Task UnirPruebaLocalAsync() => ConectarAsync();

    private async Task ConectarAsync()
    {
        if (equipo.SelectedIndex < 0)
        {
            estado.Text = $"Sin conectar · Cuenta: {sesion.Nombre}\nSelecciona Equipo 1 o Equipo 2 antes de buscar.";
            return;
        }
        conectar.Enabled = false;
        conectar.Text = "Conectando…";
        estado.Text = $"Conectando al servidor como {sesion.Nombre}…";
        campo.Mensaje = "Conectando…";
        var destino = new UriBuilder(servidor)
        {
            Scheme = servidor.Scheme == "https" ? "wss" : "ws",
            Path = "/ws/juego",
            Query = $"cola={(compacta ? "local" : "red")}&equipo={equipo.SelectedIndex + 1}&avion={avion.SelectedItem}"
        }.Uri;
        try
        {
            socket = new ClientWebSocket();
            socket.Options.SetRequestHeader("Authorization", new AuthenticationHeaderValue("Bearer", sesion.Token).ToString());
            await socket.ConnectAsync(destino, vida.Token);
            estado.Text = $"Conectado como {sesion.Nombre} · Equipo {equipo.SelectedIndex + 1}\nEsperando rivales. La partida se inicia automáticamente.";
            conectar.Text = "En sala";
            campo.Mensaje = "SALA DE ESPERA";
            _ = LeerMensajesAsync(socket);
            await SolicitarBotsAsync();
        }
        catch (Exception error) when (error is WebSocketException or HttpRequestException or OperationCanceledException)
        {
            estado.Text = "No se pudo conectar con el servidor.";
            campo.Mensaje = "SIN CONEXIÓN";
            conectar.Enabled = true;
            conectar.Text = "Buscar partida";
            socket?.Dispose();
            socket = null;
        }
        campo.Invalidate();
    }

    private async Task SolicitarBotsAsync()
    {
        if (!completarBots.Checked || botsSolicitados || jugando || socket?.State != WebSocketState.Open)
            return;
        botsSolicitados = true;
        completarBots.Enabled = false;
        estado.Text = "Completando los puestos libres con bots…";
        try
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes("{\"t\":\"fill_bots\"}"),
                WebSocketMessageType.Text, true, vida.Token);
        }
        catch (Exception error) when (error is WebSocketException or OperationCanceledException)
        {
            botsSolicitados = false;
            completarBots.Enabled = true;
            estado.Text = "No se pudo completar la sala con bots.";
        }
    }

    private async Task LeerMensajesAsync(ClientWebSocket conexion)
    {
        var buffer = new byte[4096];
        try
        {
            while (conexion.State == WebSocketState.Open && !vida.IsCancellationRequested)
            {
                var mensaje = await conexion.ReceiveAsync(buffer, vida.Token);
                if (mensaje.MessageType == WebSocketMessageType.Close) break;
                if (mensaje.MessageType != WebSocketMessageType.Text || !mensaje.EndOfMessage) continue;
                using var documento = JsonDocument.Parse(buffer.AsMemory(0, mensaje.Count));
                var raiz = documento.RootElement;
                string tipo = raiz.GetProperty("t").GetString() ?? "";
                if (IsDisposed || !IsHandleCreated) break;
                switch (tipo)
                {
                    case "lobby":
                        int version = raiz.GetProperty("version").GetInt32();
                        int azul = raiz.GetProperty("azul").GetInt32();
                        int rojo = raiz.GetProperty("rojo").GetInt32();
                        int puesto = raiz.GetProperty("puesto").GetInt32();
                        BeginInvoke(() =>
                        {
                            if (jugando || version < ultimaVersionEspera) return;
                            ultimaVersionEspera = version;
                            estado.Text = $"Conectado · {sesion.Nombre} · Equipo {equipo.SelectedIndex + 1} · " +
                                $"Azul {azul}/3 · Rojo {rojo}/3\n" +
                                (puesto > 3 ? $"Puesto {puesto}: esperas la próxima partida." :
                                    "Con 3 por equipo inicia solo; o marca Completar con bots.");
                        });
                        break;
                    case "start":
                        int slot = raiz.GetProperty("slot").GetInt32();
                        var nombres = raiz.GetProperty("players").EnumerateArray()
                            .Select(p => p.GetProperty("Usuario").GetString() ?? "") .ToArray();
                        var tipos = raiz.GetProperty("players").EnumerateArray()
                            .Select(p => p.GetProperty("avion").GetString() ?? "Ligero").ToArray();
                        var obstaculos = raiz.GetProperty("obstacles").EnumerateArray()
                            .Select(o => o.EnumerateArray().Select(v => v.GetInt32()).ToArray()).ToArray();
                        BeginInvoke(() =>
                        {
                            campo.Iniciar(slot, nombres, tipos, obstaculos);
                            Text = $"{sesion.Nombre} · Equipo {(slot < 3 ? "1 (Azul)" : "2 (Rojo)")}";
                            estado.Text = $"Conectado · {sesion.Nombre} · Equipo {(slot < 3 ? "1 (Azul)" : "2 (Rojo)")}\n" +
                                "Tu avión tiene círculo blanco. Flechas/WASD y espacio.";
                            jugando = true;
                            completarBots.Enabled = false;
                            controles.Start();
                            campo.Focus();
                        });
                        break;
                    case "f":
                        var pilotos = raiz.GetProperty("p").EnumerateArray()
                            .Select(p => p.EnumerateArray().Select(v => v.GetInt32()).ToArray()).ToArray();
                        var balas = raiz.GetProperty("b").EnumerateArray()
                            .Select(p => p.EnumerateArray().Select(v => v.GetInt32()).ToArray()).ToArray();
                        BeginInvoke(() => campo.Actualizar(pilotos, balas));
                        break;
                    case "end":
                        string ganador = raiz.GetProperty("winner").GetString() ?? "";
                        int id = raiz.GetProperty("partidaId").GetInt32();
                        BeginInvoke(() =>
                        {
                            jugando = false;
                            controles.Stop();
                            campo.Mensaje = $"GANÓ EL EQUIPO {ganador.ToUpperInvariant()}";
                            campo.Invalidate();
                            estado.Text = $"Resultado guardado en MySQL · partida #{id}";
                        });
                        await conexion.CloseAsync(WebSocketCloseStatus.NormalClosure, "Partida finalizada", vida.Token);
                        return;
                    case "error":
                        string error = raiz.GetProperty("mensaje").GetString() ?? "Error";
                        BeginInvoke(() =>
                        {
                            estado.Text = $"No se pudo entrar: {error}";
                            campo.Mensaje = "SIN CONEXIÓN";
                            campo.Invalidate();
                        });
                        break;
                    case "info":
                        string aviso = raiz.GetProperty("mensaje").GetString() ?? "No se pudo completar la sala.";
                        BeginInvoke(() =>
                        {
                            estado.Text = aviso;
                            botsSolicitados = false;
                            completarBots.Checked = false;
                            completarBots.Enabled = true;
                        });
                        break;
                }
            }
        }
        catch (Exception error) when (error is WebSocketException or OperationCanceledException or JsonException) { }
        finally
        {
            if (!IsDisposed && IsHandleCreated)
                BeginInvoke(() =>
                {
                    jugando = false;
                    controles.Stop();
                    if (campo.Mensaje is null) estado.Text = "Se perdió la conexión.";
                    conectar.Enabled = true;
                    conectar.Text = "Buscar partida";
                    botsSolicitados = false;
                    completarBots.Enabled = true;
                });
            conexion.Dispose();
            if (ReferenceEquals(socket, conexion)) socket = null;
        }
    }

    private async Task EnviarControlesAsync()
    {
        if (!jugando || enviando || socket?.State != WebSocketState.Open) return;
        enviando = true;
        try
        {
            int x = (teclas.Contains(Keys.Right) || teclas.Contains(Keys.D) ? 1 : 0) -
                (teclas.Contains(Keys.Left) || teclas.Contains(Keys.A) ? 1 : 0);
            int y = (teclas.Contains(Keys.Down) || teclas.Contains(Keys.S) ? 1 : 0) -
                (teclas.Contains(Keys.Up) || teclas.Contains(Keys.W) ? 1 : 0);
            string json = JsonSerializer.Serialize(new { t = "input", x, y, f = teclas.Contains(Keys.Space) });
            await socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, vida.Token);
        }
        catch (Exception error) when (error is WebSocketException or OperationCanceledException) { jugando = false; }
        finally { enviando = false; }
    }
}

internal sealed class NetworkCanvas : Control
{
    private readonly Image avion = Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "condor-fighter.png"));
    private readonly Font texto = new("Segoe UI", 14, FontStyle.Bold);
    private int slot = -1;
    private string[] nombres = [];
    private string[] tipos = [];
    private int[][] obstaculos = [];
    private int[][] pilotos = [];
    private int[][] balas = [];
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string? Mensaje { get; set; } = "SALA DE ESPERA";

    public NetworkCanvas()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
    }

    public void Iniciar(int propio, string[] usuarios, string[] aeronaves, int[][] bloques)
    {
        slot = propio;
        nombres = usuarios;
        tipos = aeronaves;
        obstaculos = bloques;
        Mensaje = null;
        Invalidate();
    }

    public void Actualizar(int[][] posiciones, int[][] proyectiles)
    {
        pilotos = posiciones;
        balas = proyectiles;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(Color.FromArgb(13, 28, 45));
        float escala = Math.Min(ClientSize.Width / TeamBattle.Ancho, ClientSize.Height / TeamBattle.Alto);
        g.TranslateTransform((ClientSize.Width - TeamBattle.Ancho * escala) / 2,
            (ClientSize.Height - TeamBattle.Alto * escala) / 2);
        g.ScaleTransform(escala, escala);
        using var fondo = new SolidBrush(Color.FromArgb(24, 50, 69));
        g.FillRectangle(fondo, 0, 0, TeamBattle.Ancho, TeamBattle.Alto);
        foreach (var bloque in obstaculos)
        {
            using var brocha = new SolidBrush(Color.FromArgb(90, 105, 117));
            g.FillRectangle(brocha, bloque[0], bloque[1], bloque[2], bloque[3]);
        }
        foreach (var bala in balas)
            g.FillRectangle(bala[2] == 0 ? Brushes.Cyan : Brushes.OrangeRed, bala[0] - 3, bala[1] - 7, 6, 14);
        foreach (var p in pilotos)
        {
            int i = p[0];
            if (p[3] <= 0 || i >= nombres.Length) continue;
            bool pesado = tipos[i] == "Pesado";
            int ancho = pesado ? 46 : 36;
            int alto = pesado ? 54 : 44;
            var previo = g.Save();
            g.TranslateTransform(p[1], p[2]);
            if (i >= 3) g.RotateTransform(180);
            g.DrawImage(avion, -ancho / 2, -alto / 2, ancho, alto);
            g.Restore(previo);
            using var pincel = new SolidBrush(i == slot ? Color.White : i < 3 ? Color.Cyan : Color.OrangeRed);
            g.DrawString($"{nombres[i]}  {p[3]} HP", texto, pincel, p[1] - 44, p[2] - alto / 2 - 27);
            if (i == slot) g.DrawEllipse(Pens.White, p[1] - ancho / 2 - 4, p[2] - alto / 2 - 4, ancho + 8, alto + 8);
        }
        if (Mensaje is not null)
        {
            using var centrado = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(Mensaje, texto, Brushes.White, new RectangleF(0, 250, 560, 120), centrado);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { avion.Dispose(); texto.Dispose(); }
        base.Dispose(disposing);
    }
}
