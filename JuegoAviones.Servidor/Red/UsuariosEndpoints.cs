using JuegoAviones.Contracts.DTO;
using JuegoAviones.Core.CasosDeUso;
using JuegoAviones.Servidor.Sesiones;

namespace JuegoAviones.Servidor.Red;

public static class UsuariosEndpoints
{
    public static IEndpointRouteBuilder MapUsuarios(this IEndpointRouteBuilder endpoints)
    {
        var grupo = endpoints.MapGroup("/api/usuarios").WithTags("Usuarios");

        grupo.MapPost("/registrar", async (CredencialesRequest request, UsuarioService usuarios,
            SesionService sesiones, CancellationToken cancellationToken) =>
        {
            try
            {
                var usuario = await usuarios.RegistrarAsync(request.Nombre, request.Clave, cancellationToken);
                var sesion = sesiones.Crear(usuario);
                return Results.Ok(new SesionResponse(sesion.Nombre, sesion.Token, sesion.VenceUtc));
            }
            catch (ArgumentException error) { return Results.BadRequest(new { error = error.Message }); }
            catch (InvalidOperationException error) { return Results.Conflict(new { error = error.Message }); }
        });

        grupo.MapPost("/login", async (CredencialesRequest request, UsuarioService usuarios,
            SesionService sesiones, CancellationToken cancellationToken) =>
        {
            var usuario = await usuarios.IngresarAsync(request.Nombre, request.Clave, cancellationToken);
            if (usuario is null) return Results.Unauthorized();
            var sesion = sesiones.Crear(usuario);
            return Results.Ok(new SesionResponse(sesion.Nombre, sesion.Token, sesion.VenceUtc));
        });

        grupo.MapGet("/yo", (HttpRequest request, SesionService sesiones) =>
        {
            var sesion = sesiones.ResolverBearer(request.Headers.Authorization);
            return sesion is null ? Results.Unauthorized() : Results.Ok(new { sesion.Nombre, sesion.VenceUtc });
        });

        return endpoints;
    }
}
