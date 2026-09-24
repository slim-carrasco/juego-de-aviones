## Estructura
```text
juego-de-aviones/
├─ JuegoAviones.Core/                  # Reglas del juego + orquestación (fusión Domain+Application)
│  ├─ Entidades/
│  ├─ Reglas/
│  ├─ CasosDeUso/
│  └─ Interfaces/
│
├─ JuegoAviones.Contracts/             # Contratos compartidos cliente-servidor
│  ├─ MensajesRed/
│  ├─ DTO/
│  └─ Enums/
│
├─ JuegoAviones.Datos/                 # PostgreSQL
│  ├─ Entidades/
│  ├─ Repositorios/
│  ├─ Conexion/
│  └─ Migrations/
│
├─ JuegoAviones.Cliente/               # WinForms
│  ├─ Formularios/
│  ├─ Render/
│  └─ Input/
│
├─ JuegoAviones.Servidor/              # Consola, sin GUI
│  ├─ Red/
│  └─ Sesiones/
│
├─ JuegoAviones.Tests/
│
├─ infra/
│  ├─ docker-compose.yml
│  └─ terraform/
│
└─ docs/
```
## Roles
- **Backend**: mueve el código existente hacia Core (ImpactarTick, CrearMisil, CrearNave → reglas y entidades), y deja Form1.cs solo con lo visual, llamando a Core
- **Frontend**: sin código todavía — el juego debe seguir viéndose y jugándose exactamente igual que antes, no hay pantallas nuevas en esta fase
- **Arquitecto de BD**: sin migración de datos todavía (el guardado en resultado.txt no se toca en Fase A) — su tarea concreta ahora es dejar listo infra/docker-compose.yml con Postgres para que el equipo lo tenga disponible desde ya
- **Tester**: sin tests todavía contra código real — decide si serán automatizados o manuales, y espera a que Core exista aislado
- **Sprint**: repositorio, ramas, CI básico (solo compilar)
