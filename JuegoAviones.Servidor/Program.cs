using JuegoAviones.Core.CasosDeUso;
using JuegoAviones.Core.Interfaces;
using JuegoAviones.Data.Conexion;
using JuegoAviones.Data.Repositorios;
using JuegoAviones.Servidor.Red;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<JuegoDbContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("Juego")));
builder.Services.AddScoped<IPartidaRepository, PartidaRepository>();
builder.Services.AddScoped<PartidaService>();
builder.Services.AddProblemDetails();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<JuegoDbContext>();
	await db.Database.EnsureCreatedAsync();
}

app.UseExceptionHandler();
app.MapGet("/", () => Results.Ok(new
{
	mensaje = "API de Juego de Aviones funcionando",
	health = "/health",
	registrarPartida = "POST /api/partidas",
	ranking = "/api/partidas/ranking"
}));
app.MapGet("/health", () => Results.Ok(new { estado = "ok" }));
app.MapPartidas();

await app.RunAsync();

public partial class Program;
