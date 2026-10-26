namespace JuegoAviones.Contracts.DTO;

public sealed record CredencialesRequest(string Nombre, string Clave);
public sealed record SesionResponse(string Nombre, string Token, DateTime VenceUtc);
