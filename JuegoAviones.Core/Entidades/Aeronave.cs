using System.Drawing;

namespace JuegoAviones.Core.Entidades;

// Una clase representa lo que cada avión tiene (atributos) y hace (métodos).
public abstract class Aeronave
{
    public string Nombre { get; }
    public PointF Posicion { get; private set; }
    public SizeF Tamano { get; }
    public int Vida { get; private set; }
    public int VidaInicial { get; }
    public bool EsRival { get; }
    public abstract int AnguloRotacion { get; }
    public abstract Projectile CrearDisparo();
    public RectangleF Limites => new(
        Posicion.X - Tamano.Width / 2, Posicion.Y - Tamano.Height / 2,
        Tamano.Width, Tamano.Height);

    protected Aeronave(string nombre, PointF posicion, SizeF tamano, int vida, bool esRival)
    {
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        if (vida <= 0) throw new ArgumentOutOfRangeException(nameof(vida));
        if (!float.IsFinite(tamano.Width) || !float.IsFinite(tamano.Height) || tamano.Width <= 0 || tamano.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(tamano));
        Nombre = nombre;
        Tamano = tamano;
        Vida = VidaInicial = vida;
        EsRival = esRival;
        MoverA(posicion.X, posicion.Y);
    }

    public void MoverA(float x, float y)
    {
        if (!float.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x));
        if (!float.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(y));
        Posicion = new PointF(x, y);
    }

    public void RecibirImpacto(int dano)
    {
        if (dano <= 0) throw new ArgumentOutOfRangeException(nameof(dano));
        Vida = Math.Max(0, Vida - dano);
    }
}
