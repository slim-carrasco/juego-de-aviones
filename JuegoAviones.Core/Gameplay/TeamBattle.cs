using System.Drawing;

namespace JuegoAviones.Core.Gameplay;

public enum Equipo { Azul, Rojo }
public enum TipoAvion { Ligero, Pesado }
public enum EstadoBatalla { Jugando, Terminada }

public readonly record struct EntradaBatalla(int Horizontal, int Vertical, bool Disparar);

public sealed class PilotoBatalla
{
    internal PilotoBatalla(int indice, string usuario, Equipo equipo, TipoAvion avion, float x, float y)
    {
        Indice = indice;
        Usuario = usuario;
        Equipo = equipo;
        Avion = avion;
        X = x;
        Y = y;
        VidaMaxima = avion == TipoAvion.Pesado ? 30 : 20;
        Vida = VidaMaxima;
    }

    public int Indice { get; }
    public string Usuario { get; }
    public Equipo Equipo { get; }
    public TipoAvion Avion { get; }
    public float X { get; internal set; }
    public float Y { get; internal set; }
    public int Vida { get; internal set; }
    public int VidaMaxima { get; }
    public int Derribos { get; internal set; }
    public int Disparos { get; internal set; }
    public int Aciertos { get; internal set; }
    public bool Conectado { get; internal set; } = true;
    public RectangleF Limites => LimitesEn(X, Y);

    internal EntradaBatalla Entrada { get; set; }
    internal float EsperaDisparo { get; set; }
    internal RectangleF LimitesEn(float x, float y)
    {
        float ancho = Avion == TipoAvion.Pesado ? 46 : 36;
        float alto = Avion == TipoAvion.Pesado ? 54 : 44;
        return new RectangleF(x - ancho / 2, y - alto / 2, ancho, alto);
    }
}

public sealed class BalaBatalla(float x, float y, int tirador, Equipo equipo)
{
    public float X { get; internal set; } = x;
    public float Y { get; internal set; } = y;
    public int Tirador { get; } = tirador;
    public Equipo Equipo { get; } = equipo;
    public RectangleF Limites => new(X - 3, Y - 7, 6, 14);
}

// Este estado solo se modifica en el servidor; los clientes reciben copias para dibujar.
public sealed class TeamBattle
{
    public const float Ancho = 560;
    public const float Alto = 640;
    private readonly List<PilotoBatalla> pilotos;
    private readonly List<BalaBatalla> balas = [];
    private readonly RectangleF[] obstaculos =
    [
        new RectangleF(185, 300, 40, 40),
        new RectangleF(335, 300, 40, 40)
    ];

    public IReadOnlyList<PilotoBatalla> Pilotos => pilotos;
    public IReadOnlyList<BalaBatalla> Balas => balas;
    public IReadOnlyList<RectangleF> Obstaculos => obstaculos;
    public EstadoBatalla Estado { get; private set; } = EstadoBatalla.Jugando;
    public Equipo? Ganador { get; private set; }
    public float DuracionSegundos { get; private set; }
    public int TickNumero { get; private set; }

    public TeamBattle(IReadOnlyList<(string Usuario, Equipo Equipo, TipoAvion Avion)> participantes)
    {
        if (participantes.Count != 6 ||
            participantes.Count(p => p.Equipo == Equipo.Azul) != 3 ||
            participantes.Count(p => p.Equipo == Equipo.Rojo) != 3 ||
            participantes.Any(p => string.IsNullOrWhiteSpace(p.Usuario)) ||
            participantes.Select(p => p.Usuario).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 6)
            throw new ArgumentException("Una batalla requiere tres usuarios distintos por equipo.", nameof(participantes));

        int azul = 0;
        int rojo = 0;
        pilotos = participantes.Select((p, indice) =>
        {
            int lugar = p.Equipo == Equipo.Azul ? azul++ : rojo++;
            float x = 130 + lugar * 150;
            float y = p.Equipo == Equipo.Azul ? 548 : 92;
            return new PilotoBatalla(indice, p.Usuario, p.Equipo, p.Avion, x, y);
        }).ToList();
    }

    public void RecibirEntrada(int indice, EntradaBatalla entrada)
    {
        if (indice < 0 || indice >= pilotos.Count) return;
        pilotos[indice].Entrada = new EntradaBatalla(
            Math.Clamp(entrada.Horizontal, -1, 1),
            Math.Clamp(entrada.Vertical, -1, 1),
            entrada.Disparar);
    }

    public void Desconectar(int indice)
    {
        if (indice < 0 || indice >= pilotos.Count || Estado != EstadoBatalla.Jugando) return;
        var piloto = pilotos[indice];
        piloto.Conectado = false;
        piloto.Vida = 0;
        piloto.Entrada = default;
        RevisarGanador();
    }

    public void Actualizar(float segundos)
    {
        if (Estado != EstadoBatalla.Jugando || !float.IsFinite(segundos) || segundos <= 0) return;
        float dt = Math.Min(segundos, .05f);
        DuracionSegundos += dt;
        TickNumero++;

        foreach (var piloto in pilotos)
        {
            if (!piloto.Conectado || piloto.Vida == 0) continue;
            piloto.EsperaDisparo = Math.Max(0, piloto.EsperaDisparo - dt);
            float dx = piloto.Entrada.Horizontal;
            float dy = piloto.Entrada.Vertical;
            float magnitud = MathF.Sqrt(dx * dx + dy * dy);
            if (magnitud > 1) { dx /= magnitud; dy /= magnitud; }
            float velocidad = piloto.Avion == TipoAvion.Pesado ? 190 : 255;
            float x = Math.Clamp(piloto.X + dx * velocidad * dt, 25, Ancho - 25);
            float y = Math.Clamp(piloto.Y + dy * velocidad * dt, 28, Alto - 28);
            var limites = piloto.LimitesEn(x, y);
            if (!obstaculos.Any(obstaculo => obstaculo.IntersectsWith(limites)) &&
                !pilotos.Any(otro => otro != piloto && otro.Vida > 0 && otro.Limites.IntersectsWith(limites)))
            {
                piloto.X = x;
                piloto.Y = y;
            }

            if (piloto.Entrada.Disparar && piloto.EsperaDisparo <= 0 && balas.Count < 24)
            {
                float origenY = piloto.Y + (piloto.Equipo == Equipo.Azul ? -27 : 27);
                balas.Add(new BalaBatalla(piloto.X, origenY, piloto.Indice, piloto.Equipo));
                piloto.Disparos++;
                piloto.EsperaDisparo = piloto.Avion == TipoAvion.Pesado ? .38f : .25f;
            }
        }

        for (int i = balas.Count - 1; i >= 0 && Estado == EstadoBatalla.Jugando; i--)
        {
            var bala = balas[i];
            bala.Y += (bala.Equipo == Equipo.Azul ? -410 : 410) * dt;
            if (bala.Y < -15 || bala.Y > Alto + 15 ||
                obstaculos.Any(obstaculo => obstaculo.IntersectsWith(bala.Limites)))
            {
                balas.RemoveAt(i);
                continue;
            }

            var objetivo = pilotos.FirstOrDefault(p => p.Equipo != bala.Equipo && p.Vida > 0 &&
                p.Limites.IntersectsWith(bala.Limites));
            if (objetivo is null) continue;
            balas.RemoveAt(i);
            var tirador = pilotos[bala.Tirador];
            tirador.Aciertos++;
            objetivo.Vida = Math.Max(0, objetivo.Vida - 10);
            if (objetivo.Vida == 0)
            {
                tirador.Derribos++;
                RevisarGanador();
            }
        }
    }

    private void RevisarGanador()
    {
        bool azulVivo = pilotos.Any(p => p.Equipo == Equipo.Azul && p.Vida > 0);
        bool rojoVivo = pilotos.Any(p => p.Equipo == Equipo.Rojo && p.Vida > 0);
        if (azulVivo && rojoVivo) return;
        Estado = EstadoBatalla.Terminada;
        Ganador = azulVivo ? Equipo.Azul : Equipo.Rojo;
        balas.Clear();
    }
}
