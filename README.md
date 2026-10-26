# Juego de aeronaves

Juego de combate aéreo en C# para Windows Forms. Incluye modos individual, multijugador local de dos personas y **3 contra 3 en red**. En red, el servidor calcula posiciones, disparos, colisiones y ganador; cada cliente envía controles y dibuja el estado recibido.

## Ubicaciones

- Omarchy: `/home/joel/Work/juego-de-aviones`.
- Carpeta compartida real: `/home/joel/Windows/juego-de-aviones`.
- VM Windows: `Z:\juego-de-aviones` o `\\host.lan\Data\juego-de-aviones`.

Las tres rutas muestran los mismos archivos. Windows necesita el SDK de **.NET 10**.

## MySQL y servidor

Inicia MySQL desde Omarchy:

```bash
cd /home/joel/Work/juego-de-aviones
docker compose -f infra/docker-compose.yml up -d --wait
```

En la VM abre `iniciar-servidor.bat`. La API escucha dentro de la VM en `http://0.0.0.0:5080`; comprueba `curl http://localhost:5080/health` desde CMD. `ejecutar.bat` también inicia el servidor si aún no responde. La VM se conecta a MySQL en `172.17.0.1:3307`; Omarchy usa `localhost:3307`. MySQL solo publica ese puerto en la red interna de la VM y en `127.0.0.1`, no en la red Wi-Fi. Las tablas se crean sin borrar resultados anteriores. Las contraseñas se almacenan con hash PBKDF2.

## Cuentas de prueba

Al arrancar, el servidor crea seis cuentas si faltan. Cada contraseña coincide con el nombre:

| Usuario | Contraseña |
| --- | --- |
| `prueba1` | `prueba1` |
| `prueba2` | `prueba2` |
| `prueba3` | `prueba3` |
| `prueba4` | `prueba4` |
| `prueba5` | `prueba5` |
| `prueba6` | `prueba6` |

Cada jugador en red usa una cuenta distinta. La pantalla de acceso también permite crear cuentas.

## Probar seis jugadores en una sola PC

En la VM, abre `ejecutar.bat`, inicia sesión y pulsa **3 vs 3 · esta PC**. Solo al elegir ese botón se abren seis ventanas compactas; cada una entra automáticamente con una cuenta `prueba1` a `prueba6` y busca partida. Las tres primeras se asignan al Equipo 1 (Azul) y las otras tres al Equipo 2 (Rojo). Haz clic en una ventana para controlar ese avión con flechas o WASD y espacio. Cierra las ventanas para liberar las cuentas.

## Jugar entre laptops

En cada laptop Windows se abre **un cliente** y se pulsa **3 vs 3 · otras laptops**. Cada persona inicia sesión con una cuenta diferente, elige avión **Ligero** o **Pesado**, selecciona **Equipo 1 (Azul)** o **Equipo 2 (Rojo)** y pulsa **Buscar partida**. Todas las ventanas muestran la conexión y cuántos jugadores hay en cada equipo. La partida comienza automáticamente cuando ambos equipos tienen tres clientes. Si hay menos de seis personas, esperen a que entren las disponibles y una de ellas marca **Completar con bots**: el servidor llena los puestos libres de ambos equipos e inicia la partida. También se puede marcar antes de buscar para jugar una persona contra cinco bots. Las búsquedas de esta modalidad se mantienen separadas de la prueba de seis ventanas.

Cada equipo tiene tres lugares por partida. Si llegan más jugadores, esperan la siguiente sala. Se admiten varias salas. Flechas o WASD mueven el avión y espacio dispara.

Cuando los tres aviones de un equipo quedan fuera, el servidor guarda ganador, duración y estadísticas de los seis jugadores en `partidas_equipo` y `detalle_jugador_partida` de MySQL.

Para preparar otras laptops, ejecuta `publicar-cliente.bat` una vez en la VM y copia la carpeta `dist\cliente-windows` a cada laptop. Allí abre `JuegoAviones.exe`; no necesitan SDK ni el repositorio. En **Servidor** escribirán `http://IP_DE_OMARCHY:5080`. **El acceso desde la red Wi-Fi está cerrado por defecto** porque las cuentas de prueba usan contraseñas conocidas y HTTP no cifra el acceso. Cuando se pruebe entre laptops, activa un puente temporal solo en una red privada y ciérralo al terminar. No se copia MySQL ni se ejecuta un servidor por laptop.

Los modos **Solo** y **Duelo local (2)** siguen disponibles. En el duelo local J1 usa flechas y espacio; J2 usa WASD y F. `P`/`Esc` pausa, `R` reinicia y `Enter` inicia o reinicia.

## Compilar y validar

En Windows:

```cmd
cd /d Z:\juego-de-aviones
dotnet build JuegoAviones.slnx
dotnet test JuegoAviones.Tests\JuegoAviones.Tests.csproj
```

Desde Omarchy se puede compilar para Windows con `dotnet build JuegoAviones.slnx -p:EnableWindowsTargeting=true`.

`JuegoAviones.Core` contiene reglas y simulación; `JuegoAviones.Servidor` controla salas y API; `JuegoAviones.Data` persiste en MySQL; `JuegoAviones.Contracts` contiene mensajes HTTP; `JuegoAviones` es el cliente Windows Forms. El servidor genera 30 cuadros por segundo y limita cada uno a 1024 bytes. `FlightCanvas` muestra profiling del juego local. Ver [diseño del avión](docs/DISENO-AVION.md) y [guía de exposición](docs/EXPOSICION-5-PERSONAS.md).
