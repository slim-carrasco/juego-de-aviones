namespace JuegoAviones.Servidor.Sesiones;

public sealed record SesionJuego(string Token, int UsuarioId, string Nombre, DateTime VenceUtc);
