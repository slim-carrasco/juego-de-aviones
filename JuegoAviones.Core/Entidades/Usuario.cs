namespace JuegoAviones.Core.Entidades;

public sealed class Usuario
{
    private Usuario() { }

    public int Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string ClaveHash { get; private set; } = string.Empty;
    public DateTime FechaUtc { get; private set; }

    public static Usuario Crear(string nombre, string claveHash)
    {
        nombre = nombre?.Trim() ?? string.Empty;
        if (nombre.Length is < 3 or > 30)
            throw new ArgumentException("El usuario debe tener entre 3 y 30 caracteres.", nameof(nombre));
        if (string.IsNullOrWhiteSpace(claveHash))
            throw new ArgumentException("La clave es obligatoria.", nameof(claveHash));
        return new Usuario { Nombre = nombre, ClaveHash = claveHash, FechaUtc = DateTime.UtcNow };
    }
}
