using JuegoAviones.Contracts.DTO;
using JuegoAviones.Core.CasosDeUso;
using JuegoAviones.Core.Entidades;

namespace JuegoAviones.Servidor.Red;

public static class PartidasEndpoints
{
    public static IEndpointRouteBuilder MapPartidas(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/partidas").WithTags("Partidas");

        grupo.MapPost("", async (
            RegistrarPartidaRequest request,
            PartidaService service,
            CancellationToken cancellationToken) =>
        {
            if (!Enum.TryParse<ResultadoPartida>(request.Resultado, ignoreCase: false, out var resultado))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Resultado)] = ["El resultado debe ser Ganaste o Perdiste."]
                });

            try
            {
                var partida = await service.RegistrarAsync(
                    request.Jugador,
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
        partida.FechaUtc);
}
