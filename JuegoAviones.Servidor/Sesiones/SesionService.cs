using System.Collections.Concurrent;
using System.Security.Cryptography;
using JuegoAviones.Core.Entidades;

namespace JuegoAviones.Servidor.Sesiones;

public sealed class SesionService
{
    private readonly ConcurrentDictionary<string, SesionJuego> sesiones = new();

    public SesionJuego Crear(Usuario usuario)
    {
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var sesion = new SesionJuego(token, usuario.Id, usuario.Nombre, DateTime.UtcNow.AddHours(12));
        sesiones[token] = sesion;
        return sesion;
    }

    public SesionJuego? Resolver(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || !sesiones.TryGetValue(token, out var sesion)) return null;
        if (sesion.VenceUtc > DateTime.UtcNow) return sesion;
        sesiones.TryRemove(token, out _);
        return null;
    }

    public SesionJuego? ResolverBearer(string? authorization) =>
        authorization is not null && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? Resolver(authorization[7..].Trim())
            : null;
}
