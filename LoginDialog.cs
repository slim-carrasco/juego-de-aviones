using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JuegoAviones.Contracts.DTO;

namespace JuegoAviones;

public sealed class LoginDialog : Form
{
    private readonly HttpClient api = new()
    {
        BaseAddress = new Uri("http://localhost:5080"),
        Timeout = TimeSpan.FromSeconds(5)
    };
    private readonly CancellationTokenSource lifetime = new();
    private readonly TextBox nombre = new() { Width = 230 };
    private readonly TextBox clave = new() { Width = 230, UseSystemPasswordChar = true };
    private readonly TextBox servidor = new() { Width = 230, Text = Environment.GetEnvironmentVariable("JUEGO_API_URL") ?? "http://localhost:5080" };
    private readonly Label estado = new() { Dock = DockStyle.Fill, ForeColor = Color.White,
        TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button entrar = new() { Text = "Entrar al juego", Width = 150 };
    private readonly Button registrar = new() { Text = "Crear cuenta", Width = 130 };

    public SesionResponse? Sesion { get; private set; }
    public Uri Servidor { get; private set; } = new("http://localhost:5080");

    public LoginDialog()
    {
        UiTheme.StyleTitleBar(this);
        Text = "Acceso al juego";
        ClientSize = new Size(390, 246);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UiTheme.Surface;
        ForeColor = UiTheme.Text;
        foreach (var campo in new[] { nombre, clave, servidor })
        {
            campo.BackColor = UiTheme.Input;
            campo.ForeColor = UiTheme.Text;
            campo.BorderStyle = BorderStyle.FixedSingle;
        }

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 5
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { Text = "Usuario", AutoSize = true, Anchor = AnchorStyles.Left,
            ForeColor = UiTheme.Text }, 0, 0);
        panel.Controls.Add(nombre, 1, 0);
        panel.Controls.Add(new Label { Text = "Clave", AutoSize = true, Anchor = AnchorStyles.Left,
            ForeColor = UiTheme.Text }, 0, 1);
        panel.Controls.Add(clave, 1, 1);
        panel.Controls.Add(new Label { Text = "Servidor", AutoSize = true, Anchor = AnchorStyles.Left,
            ForeColor = UiTheme.Text }, 0, 2);
        panel.Controls.Add(servidor, 1, 2);
        UiTheme.StyleButton(entrar, accent: true);
        UiTheme.StyleButton(registrar);
        entrar.Click += async (_, _) => await AccederAsync(false);
        registrar.Click += async (_, _) => await AccederAsync(true);
        var botones = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
        botones.Controls.AddRange([entrar, registrar]);
        panel.Controls.Add(botones, 0, 3);
        panel.SetColumnSpan(botones, 2);
        panel.Controls.Add(estado, 0, 4);
        panel.SetColumnSpan(estado, 2);
        Controls.Add(panel);
        AcceptButton = entrar;
        ActiveControl = nombre;
        FormClosed += (_, _) => { lifetime.Cancel(); api.Dispose(); lifetime.Dispose(); };
    }

    private async Task AccederAsync(bool crear)
    {
        if (nombre.Text.Trim().Length is < 3 or > 30 || clave.Text.Length is < 6 or > 128)
        {
            estado.Text = "Completa el usuario y la contraseña.";
            return;
        }
        if (!Uri.TryCreate(servidor.Text.Trim().TrimEnd('/'), UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            estado.Text = "Escribe una dirección http:// válida.";
            return;
        }
        Servidor = uri;
        entrar.Enabled = false;
        registrar.Enabled = false;
        estado.Text = "Conectando con el servidor…";
        try
        {
            using var respuesta = await api.PostAsJsonAsync(new Uri(Servidor, crear ? "/api/usuarios/registrar" : "/api/usuarios/login"),
                new CredencialesRequest(nombre.Text.Trim(), clave.Text), lifetime.Token);
            if (!respuesta.IsSuccessStatusCode)
            {
                estado.Text = respuesta.StatusCode == HttpStatusCode.Unauthorized
                    ? "Usuario o contraseña incorrectos."
                    : respuesta.StatusCode == HttpStatusCode.Conflict ? "Ese nombre ya existe." :
                      $"Acceso rechazado: {(int)respuesta.StatusCode}";
                return;
            }
            Sesion = await respuesta.Content.ReadFromJsonAsync<SesionResponse>(cancellationToken: lifetime.Token)
                ?? throw new InvalidOperationException("Respuesta de acceso vacía.");
            DialogResult = DialogResult.OK;
        }
        catch (HttpRequestException) { estado.Text = "Servidor sin conexión; inicia la API."; }
        catch (OperationCanceledException) { if (!IsDisposed) estado.Text = "Tiempo de espera agotado."; }
        catch (InvalidOperationException error) { estado.Text = error.Message; }
        catch (JsonException) { estado.Text = "Respuesta inválida del servidor."; }
        finally { if (!IsDisposed) { entrar.Enabled = true; registrar.Enabled = true; } }
    }
}
