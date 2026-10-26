using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using JuegoAviones.Core.Entidades;
using JuegoAviones.Core.Gameplay;
using JuegoAviones.Core.Interfaces;

namespace JuegoAviones.Servidor.Red;

// Una sala conserva el estado autorizado. Los clientes solo envían controles.
public sealed class SalasEnRed(IServiceScopeFactory scopes, ILogger<SalasEnRed> log)
{
    private readonly object gate = new();
    private readonly List<JugadorRed> espera = [];
    private readonly HashSet<string> ocupados = new(StringComparer.OrdinalIgnoreCase);
    private int versionEspera;

    public async Task AtenderAsync(WebSocket socket, string usuario, string cola, int equipo,
        TipoAvion avion, CancellationToken cancelacion)
    {
        var jugador = new JugadorRed(socket, usuario, cola, equipo, avion);
        List<SalaRed> nuevas;
        bool rechazado;
        lock (gate)
        {
            rechazado = ocupados.Contains(usuario);
            if (rechazado)
            {
                nuevas = [];
            }
            else
            {
                espera.Add(jugador);
                ocupados.Add(usuario);
                nuevas = FormarSalas();
                versionEspera++;
            }
        }
        if (rechazado)
        {
            await jugador.EnviarAsync(new { t = "error", mensaje = "Esta cuenta ya está en otra ventana de juego." }, cancelacion);
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Cuenta ocupada", CancellationToken.None);
            return;
        }
        foreach (var sala in nuevas) _ = sala.EjecutarAsync();
        await PublicarEsperaAsync(cola);

        var buffer = new byte[256];
        try
        {
            while (socket.State == WebSocketState.Open && !cancelacion.IsCancellationRequested)
            {
                var resultado = await socket.ReceiveAsync(buffer, cancelacion);
                if (resultado.MessageType == WebSocketMessageType.Close) break;
                if (resultado.MessageType != WebSocketMessageType.Text || !resultado.EndOfMessage) continue;
                try
                {
                    using var documento = JsonDocument.Parse(buffer.AsMemory(0, resultado.Count));
                    var raiz = documento.RootElement;
                    switch (raiz.GetProperty("t").GetString())
                    {
                        case "input":
                            int x = raiz.GetProperty("x").GetInt32();
                            int y = raiz.GetProperty("y").GetInt32();
                            bool disparar = raiz.GetProperty("f").GetBoolean();
                            jugador.Sala?.Recibir(jugador.Indice, new EntradaBatalla(x, y, disparar));
                            break;
                        case "fill_bots":
                            if (!CompletarConBots(jugador))
                                await jugador.EnviarAsync(new { t = "info",
                                    mensaje = "No hay un puesto disponible en esta sala; espera la próxima." }, cancelacion);
                            else
                                await PublicarEsperaAsync(cola);
                            break;
                    }
                }
                catch (JsonException) { }
                catch (KeyNotFoundException) { }
                catch (InvalidOperationException) { }
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException) { }
        finally
        {
            bool estabaEsperando;
            lock (gate)
            {
                estabaEsperando = espera.Remove(jugador);
                ocupados.Remove(usuario);
                if (estabaEsperando) versionEspera++;
            }
            jugador.Sala?.Desconectar(jugador.Indice);
            if (estabaEsperando) await PublicarEsperaAsync(cola);
        }
    }

    private bool CompletarConBots(JugadorRed solicitante)
    {
        SalaRed sala;
        lock (gate)
        {
            if (!espera.Contains(solicitante)) return false;
            var candidatos = espera.Where(p => p.Cola == solicitante.Cola).ToList();
            var azul = candidatos.Where(p => p.EquipoElegido == 1).Take(3);
            var rojo = candidatos.Where(p => p.EquipoElegido == 2).Take(3);
            var elegidos = azul.Concat(rojo).ToList();
            if (!elegidos.Contains(solicitante)) return false;
            foreach (var jugador in elegidos) espera.Remove(jugador);
            versionEspera++;
            sala = new SalaRed(elegidos, scopes, log);
        }
        _ = sala.EjecutarAsync();
        return true;
    }

    private async Task PublicarEsperaAsync(string cola)
    {
        List<(JugadorRed Jugador, int Azul, int Rojo, int Puesto)> avisos;
        int version;
        lock (gate)
        {
            version = versionEspera;
            var jugadores = espera.Where(p => p.Cola == cola).ToList();
            int azul = jugadores.Count(p => p.EquipoElegido == 1);
            int rojo = jugadores.Count(p => p.EquipoElegido == 2);
            avisos = jugadores.Select(p =>
                (p, azul, rojo, jugadores.TakeWhile(otro => otro != p)
                    .Count(otro => otro.EquipoElegido == p.EquipoElegido) + 1)).ToList();
        }
        await Task.WhenAll(avisos.Select(a => a.Jugador.EnviarAsync(new
        {
            t = "lobby", version, equipo = a.Jugador.EquipoElegido,
            azul = Math.Min(a.Azul, 3), rojo = Math.Min(a.Rojo, 3), puesto = a.Puesto
        }, CancellationToken.None)));
    }

    private List<SalaRed> FormarSalas()
    {
        var salas = new List<SalaRed>();
        foreach (var cola in espera.Select(p => p.Cola).Distinct().ToArray())
        {
            while (true)
            {
                var azul = espera.Where(p => p.Cola == cola && p.EquipoElegido == 1).Take(3).ToList();
                var rojo = espera.Where(p => p.Cola == cola && p.EquipoElegido == 2).Take(3).ToList();
                if (azul.Count != 3 || rojo.Count != 3) break;
                var jugadores = azul.Concat(rojo).ToList();
                foreach (var jugador in jugadores) espera.Remove(jugador);
                var sala = new SalaRed(jugadores, scopes, log);
                salas.Add(sala);
            }
        }
        return salas;
    }
}

internal sealed class JugadorRed(WebSocket socket, string usuario, string cola, int equipoElegido, TipoAvion avion)
{
    private readonly SemaphoreSlim envio = new(1, 1);
    public string Usuario { get; } = usuario;
    public string Cola { get; } = cola;
    public int EquipoElegido { get; } = equipoElegido;
    public TipoAvion Avion { get; } = avion;
    public SalaRed? Sala { get; set; }
    public int Indice { get; set; } = -1;

    public Task EnviarAsync<T>(T mensaje, CancellationToken cancelacion) =>
        EnviarTextoAsync(JsonSerializer.Serialize(mensaje), cancelacion);

    public async Task EnviarTextoAsync(string mensaje, CancellationToken cancelacion)
    {
        await envio.WaitAsync(cancelacion);
        try
        {
            if (socket.State == WebSocketState.Open)
                await socket.SendAsync(Encoding.UTF8.GetBytes(mensaje), WebSocketMessageType.Text, true, cancelacion);
        }
        catch (WebSocketException) { }
        catch (ObjectDisposedException) { }
        finally { envio.Release(); }
    }
}

internal sealed class SalaRed
{
    private readonly object gate = new();
    private readonly List<JugadorRed> jugadores;
    private readonly IServiceScopeFactory scopes;
    private readonly ILogger log;
    private readonly TeamBattle batalla;
    private readonly int[] indicesBots;

    public SalaRed(List<JugadorRed> jugadores, IServiceScopeFactory scopes, ILogger log)
    {
        this.jugadores = jugadores;
        this.scopes = scopes;
        this.log = log;
        var participantes = new List<(JugadorRed? Humano, string Usuario, Equipo Equipo, TipoAvion Avion)>();
        var nombres = new HashSet<string>(jugadores.Select(p => p.Usuario), StringComparer.OrdinalIgnoreCase);
        foreach (var equipo in new[] { Equipo.Azul, Equipo.Rojo })
        {
            var humanos = jugadores.Where(p => p.EquipoElegido == (equipo == Equipo.Azul ? 1 : 2)).ToList();
            participantes.AddRange(humanos.Select(p => ((JugadorRed?)p, p.Usuario, equipo, p.Avion)));
            for (int numero = humanos.Count + 1; numero <= 3; numero++)
            {
                string nombre = $"Bot {equipo} {numero}";
                while (!nombres.Add(nombre)) nombre += " Bot";
                participantes.Add((null, nombre, equipo, TipoAvion.Ligero));
            }
        }
        batalla = new TeamBattle(participantes.Select(p => (p.Usuario, p.Equipo, p.Avion)).ToList());
        indicesBots = participantes.Select((p, i) => (p.Humano, Indice: i))
            .Where(p => p.Humano is null).Select(p => p.Indice).ToArray();
        for (int i = 0; i < participantes.Count; i++)
            if (participantes[i].Humano is { } humano)
            {
                humano.Sala = this;
                humano.Indice = i;
            }
    }

    public void Recibir(int indice, EntradaBatalla entrada)
    {
        lock (gate) batalla.RecibirEntrada(indice, entrada);
    }

    public void Desconectar(int indice)
    {
        lock (gate) batalla.Desconectar(indice);
    }

    public async Task EjecutarAsync()
    {
        try
        {
            var lista = batalla.Pilotos.Select(p => new { p.Indice, p.Usuario, equipo = p.Equipo.ToString(), avion = p.Avion.ToString() }).ToArray();
            await Task.WhenAll(jugadores.Select(p => p.EnviarAsync(new
            {
                t = "start", slot = p.Indice, players = lista,
                obstacles = batalla.Obstaculos.Select(o => new[] { (int)o.X, (int)o.Y, (int)o.Width, (int)o.Height }).ToArray()
            }, CancellationToken.None)));

            using var reloj = new PeriodicTimer(TimeSpan.FromSeconds(1d / 30));
            while (await reloj.WaitForNextTickAsync())
            {
                string cuadro;
                bool finalizada;
                lock (gate)
                {
                    foreach (int indice in indicesBots)
                        batalla.RecibirEntrada(indice, BotPiloto.Decidir(batalla, indice));
                    batalla.Actualizar(1f / 30);
                    // Coordenadas enteras y límite de 24 proyectiles: menos de 1024 bytes por cuadro.
                    cuadro = JsonSerializer.Serialize(new
                    {
                        t = "f", n = batalla.TickNumero,
                        p = batalla.Pilotos.Select(p => new[] { p.Indice, (int)p.X, (int)p.Y, p.Vida, p.Derribos }).ToArray(),
                        b = batalla.Balas.Select(b => new[] { (int)b.X, (int)b.Y, b.Equipo == Equipo.Azul ? 0 : 1 }).ToArray()
                    });
                    finalizada = batalla.Estado == EstadoBatalla.Terminada;
                }
                if (Encoding.UTF8.GetByteCount(cuadro) > 1024)
                    throw new InvalidOperationException("El cuadro supera 1024 bytes.");
                await Task.WhenAll(jugadores.Select(p => p.EnviarTextoAsync(cuadro, CancellationToken.None)));
                if (finalizada) break;
            }

            using var scope = scopes.CreateScope();
            var repositorio = scope.ServiceProvider.GetRequiredService<IResultadoEquipoRepository>();
            int partidaId = await repositorio.GuardarAsync(PartidaEquipo.DesdeBatalla(batalla), CancellationToken.None);
            await Task.WhenAll(jugadores.Select(p => p.EnviarAsync(new
            {
                t = "end", winner = batalla.Ganador!.Value.ToString(), partidaId,
                duration = MathF.Round(batalla.DuracionSegundos, 1)
            }, CancellationToken.None)));
        }
        catch (Exception error) { log.LogError(error, "Falló sala de seis jugadores"); }
    }
}
