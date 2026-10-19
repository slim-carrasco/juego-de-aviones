using System.Drawing;

namespace JuegoAviones.Core.Entidades;

public sealed class Projectile
{
    public float X { get; internal set; }
    public float Y { get; internal set; }
    public bool Enemy { get; }
    public RectangleF Bounds => new(X - 3, Y - 8, 6, 16);

    internal Projectile(float x, float y, bool enemy) => (X, Y, Enemy) = (x, y, enemy);
}
