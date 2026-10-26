using JuegoAviones.Contracts.DTO;
using JuegoAviones.Core.CasosDeUso;
using JuegoAviones.Core.Entidades;
using JuegoAviones.Servidor.Sesiones;

namespace JuegoAviones.Servidor.Red;

public static class PartidasEndpoints
{
    public static IEndpointRouteBuilder MapPartidas(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/partidas").WithTags("Partidas");

        grupo.MapPost("", async (
            RegistrarPartidaRequest request,
            PartidaService service,
            SesionService sesiones,
            HttpRequest http,
            CancellationToken cancellationToken) =>
        {
            var sesion = sesiones.ResolverBearer(http.Headers.Authorization);
            if (sesion is null) return Results.Unauthorized();
            if (!string.Equals(request.Jugador, sesion.Nombre, StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "El jugador no coincide con la sesión." });
            if (!Enum.TryParse<ResultadoPartida>(request.Resultado, ignoreCase: false, out var resultado))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Resultado)] = ["El resultado debe ser Ganaste o Perdiste."]
                });

            try
            {
                var partida = await service.RegistrarAsync(
                    sesion.Nombre,
                    resultado,
                    request.VidaJugador,
                    request.VidaRival,
                    cancellationToken);

                return Results.Created($"/api/partidas/{partida.Id}", ToResponse(partida));
            }
            catch (ArgumentException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["partida"] = [exception.Message]
                });
            }
        }).WithName("RegistrarPartida");

        grupo.MapPost("/duelo", async (
            RegistrarDueloRequest request,
            PartidaService service,
            SesionService sesiones,
            HttpRequest http,
            CancellationToken cancellationToken) =>
        {
            var sesion = sesiones.ResolverBearer(http.Headers.Authorization);
            if (sesion is null) return Results.Unauthorized();
            try
            {
                var duelo = await service.RegistrarDueloAsync($"{sesion.Nombre} (J1)", $"{sesion.Nombre} (J2)",
                    request.Ganador, request.VidaJugador1, request.VidaJugador2, cancellationToken);
                return Results.Created("/api/partidas/duelo", new
                {
                    jugador1 = ToResponse(duelo.Jugador1),
                    jugador2 = ToResponse(duelo.Jugador2)
                });
            }
            catch (ArgumentException error)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["duelo"] = [error.Message] });
            }
        }).WithName("RegistrarDuelo");

        grupo.MapGet("/{id:int}", async (
            int id,
            PartidaService service,
            CancellationToken cancellationToken) =>
        {
            var partida = await service.ObtenerAsync(id, cancellationToken);
            return partida is null ? Results.NotFound() : Results.Ok(ToResponse(partida));
        });

        grupo.MapGet("/ranking", async (
            PartidaService service,
            CancellationToken cancellationToken) =>
        {
            var ranking = await service.RankingAsync(cancellationToken);
            return Results.Ok(ranking.Select(resultado => new RankingEntryResponse(
                resultado.Jugador,
                resultado.Partidas,
                resultado.Victorias,
                resultado.Derrotas)));
        });

        return endpoints;
    }

    private static PartidaResponse ToResponse(Partida partida) => new(
        partida.Id,
        partida.Jugador,
        partida.Resultado.ToString(),
        partida.VidaJugador,
        partida.VidaRival,
        partida.FechaUtc,
        partida.Modo,
        partida.Oponente);
}
