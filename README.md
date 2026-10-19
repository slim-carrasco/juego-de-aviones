# Juego de aviones

Práctica de C# con Windows Forms: integrar un avión al formulario usando programación orientada a objetos y arquitectura cliente-servidor. Se utiliza el caza moderno con transparencia de la primera propuesta, en una pantalla sencilla con escenario, jugador, rival y vidas.

La clase base `Aeronave` define atributos y métodos; `AeronaveJugador` y `AeronaveRival` implementan disparos distintos mediante polimorfismo. La pantalla muestra una medida de profiling: tiempo promedio de 60 actualizaciones de la lógica durante la partida. Core coordina el combate; el cliente dibuja las aeronaves y envía los resultados al servidor HTTP existente para guardarlos en PostgreSQL.

La explicación del dibujo y de su integración está en [docs/DISENO-AVION.md](docs/DISENO-AVION.md).

La [guía de exposición para cinco personas](docs/EXPOSICION-5-PERSONAS.md) incluye el reparto, los archivos y bloques que deben abrir, ejemplos de código, guiones breves, métricas y preguntas del docente.

## Ejecutar en Windows

Desde CMD, dentro de la carpeta del proyecto y con el SDK de .NET 10:

```cmd
dotnet build JuegoAviones.csproj --no-incremental
dotnet run --project JuegoAviones.csproj --no-build
```

Si accedes desde WSL, primero entra a la carpeta con:

```cmd
pushd \\wsl.localhost\Ubuntu\home\david\Trabajos_U\juego-de-aviones
```

Enter inicia la partida. Las flechas mueven el avión. Enter o Espacio dispara. P/Esc pausa y R reinicia. La partida también se pausa cuando la ventana pierde el foco.

El jugador comienza con 20 de vida y el rival con 50. Cada impacto resta 10. El juego se puede jugar sin servidor; para guardar resultados se necesita el backend.

## Archivos principales

- `Assets/condor-fighter.png`: imagen del caza, utilizada por ambas aeronaves.
- `JuegoAviones.Core/Entidades/Aeronave.cs`: atributos, movimiento y recepción de impactos.
- `Render/FlightCanvas.cs`: dibujo de las imágenes, nubes, proyectiles y pantalla.
- `JuegoAviones.Core/Gameplay/FlightGame.cs`: posiciones, vidas, movimiento y colisiones.
- `Form1.cs`: ventana, teclado y comunicación con el servidor.
- `JuegoAviones.Contracts`: DTO compartidos.
- `JuegoAviones.Data`: persistencia PostgreSQL con Entity Framework Core.
- `JuegoAviones.Servidor`: API HTTP de resultados y ranking.
- `JuegoAviones.Tests`: pruebas de registros y reglas del juego.

## Servidor y PostgreSQL

```powershell
docker compose -f infra/docker-compose.yml up -d
dotnet run --project JuegoAviones.Servidor/JuegoAviones.Servidor.csproj --urls http://localhost:5080
```

Comprobar estado y ranking:

```powershell
Invoke-RestMethod http://localhost:5080/health
Invoke-RestMethod http://localhost:5080/api/partidas/ranking
```

## Compilar y probar

```powershell
dotnet build JuegoAviones.slnx
dotnet test JuegoAviones.Tests/JuegoAviones.Tests.csproj
```

Windows Forms requiere Windows para ejecutar la interfaz. Desde Linux puede comprobarse la compilación con `dotnet build JuegoAviones.slnx -p:EnableWindowsTargeting=true`.
