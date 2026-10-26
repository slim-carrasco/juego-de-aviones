using JuegoAviones.Core.CasosDeUso;
using JuegoAviones.Core.Interfaces;
using JuegoAviones.Data.Conexion;
using JuegoAviones.Data.Repositorios;
using JuegoAviones.Servidor;
using JuegoAviones.Servidor.Red;
using JuegoAviones.Servidor.Sesiones;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<JuegoDbContext>(options =>
	options.UseMySQL(builder.Configuration.GetConnectionString("Juego")
	    ?? throw new InvalidOperationException("Falta ConnectionStrings:Juego.")));
builder.Services.AddScoped<IPartidaRepository, PartidaRepository>();
builder.Services.AddScoped<PartidaService>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<IResultadoEquipoRepository, ResultadoEquipoRepository>();
builder.Services.AddSingleton<JuegoAviones.Servidor.Sesiones.SesionService>();
builder.Services.AddSingleton<SalasEnRed>();
builder.Services.AddProblemDetails();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<JuegoDbContext>();
	await InicializadorMySql.PrepararAsync(db);
	var usuarios = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
	var servicio = scope.ServiceProvider.GetRequiredService<UsuarioService>();
	for (int numero = 1; numero <= CuentasPrueba.Cantidad; numero++)
	{
		string nombre = CuentasPrueba.Nombre(numero);
		if (await usuarios.ObtenerPorNombreAsync(nombre, CancellationToken.None) is null)
			await servicio.RegistrarAsync(nombre, nombre, CancellationToken.None);
	}
}

app.UseExceptionHandler();
app.UseWebSockets();
app.MapGet("/ws/juego", async (HttpContext contexto, SesionService sesiones, SalasEnRed salas) =>
{
    var sesion = sesiones.ResolverBearer(contexto.Request.Headers.Authorization);
    if (sesion is null) { contexto.Response.StatusCode = 401; return; }
    if (!contexto.WebSockets.IsWebSocketRequest) { contexto.Response.StatusCode = 400; return; }
    string cola = contexto.Request.Query["cola"].ToString();
    if (cola is not ("local" or "red")) { contexto.Response.StatusCode = 400; return; }
    if (!int.TryParse(contexto.Request.Query["equipo"], out int equipo) || equipo is < 1 or > 2 ||
        !Enum.TryParse<JuegoAviones.Core.Gameplay.TipoAvion>(contexto.Request.Query["avion"], true, out var avion))
    { contexto.Response.StatusCode = 400; return; }
    using var socket = await contexto.WebSockets.AcceptWebSocketAsync();
    await salas.AtenderAsync(socket, sesion.Nombre, cola, equipo, avion, contexto.RequestAborted);
});
app.MapGet("/", () => Results.Ok(new
{
	mensaje = "API de Juego de Aviones funcionando",
	health = "/health",
	acceso = "POST /api/usuarios/login",
	registro = "POST /api/usuarios/registrar",
	registrarPartida = "POST /api/partidas o /api/partidas/duelo",
	ranking = "/api/partidas/ranking"
}));
app.MapGet("/health", () => Results.Ok(new { estado = "ok" }));
app.MapPartidas();
app.MapUsuarios();

await app.RunAsync();

public partial class Program;
