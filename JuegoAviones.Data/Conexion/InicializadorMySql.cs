using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace JuegoAviones.Data.Conexion;

public static class InicializadorMySql
{
    public static async Task PrepararAsync(JuegoDbContext db, CancellationToken cancellationToken = default)
    {
        // EnsureCreated solo actúa cuando la base está vacía. Estas sentencias también
        // incorporan las tablas nuevas si ya hay partidas o aeronaves anteriores.
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            DbConnection conexion = db.Database.GetDbConnection();
            await EjecutarAsync(conexion, """
                CREATE TABLE IF NOT EXISTS usuarios (
                    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    Nombre VARCHAR(30) NOT NULL,
                    ClaveHash VARCHAR(150) NOT NULL,
                    FechaUtc DATETIME(6) NOT NULL,
                    UNIQUE KEY IX_usuarios_Nombre (Nombre)
                ) CHARACTER SET utf8mb4;
                """, cancellationToken);
            await EjecutarAsync(conexion, """
                CREATE TABLE IF NOT EXISTS partidas (
                    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    Jugador VARCHAR(50) NOT NULL,
                    Resultado VARCHAR(255) NOT NULL,
                    VidaJugador INT NOT NULL,
                    VidaRival INT NOT NULL,
                    FechaUtc DATETIME(6) NOT NULL,
                    Modo VARCHAR(24) NOT NULL DEFAULT 'Solo',
                    Oponente VARCHAR(50) NULL,
                    INDEX IX_partidas_Jugador_Resultado (Jugador, Resultado)
                ) CHARACTER SET utf8mb4;
                """, cancellationToken);
            if (!await ExisteColumnaAsync(conexion, "partidas", "Modo", cancellationToken))
                await EjecutarAsync(conexion,
                    "ALTER TABLE partidas ADD COLUMN Modo VARCHAR(24) NOT NULL DEFAULT 'Solo';", cancellationToken);
            if (!await ExisteColumnaAsync(conexion, "partidas", "Oponente", cancellationToken))
                await EjecutarAsync(conexion,
                    "ALTER TABLE partidas ADD COLUMN Oponente VARCHAR(50) NULL;", cancellationToken);
            await EjecutarAsync(conexion, """
                CREATE TABLE IF NOT EXISTS partidas_equipo (
                    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    EquipoGanador VARCHAR(8) NOT NULL,
                    DuracionSegundos FLOAT NOT NULL,
                    FechaUtc DATETIME(6) NOT NULL
                ) CHARACTER SET utf8mb4;
                """, cancellationToken);
            await EjecutarAsync(conexion, """
                CREATE TABLE IF NOT EXISTS detalle_jugador_partida (
                    Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                    PartidaEquipoId INT NOT NULL,
                    Usuario VARCHAR(30) NOT NULL,
                    Equipo VARCHAR(8) NOT NULL,
                    Avion VARCHAR(8) NOT NULL,
                    Derribos INT NOT NULL,
                    Disparos INT NOT NULL,
                    Aciertos INT NOT NULL,
                    VidaFinal INT NOT NULL,
                    ConectadoAlFinal TINYINT(1) NOT NULL,
                    INDEX IX_detalle_jugador_partida_PartidaEquipoId_Usuario (PartidaEquipoId, Usuario),
                    CONSTRAINT FK_detalle_jugador_partida_partidas_equipo_PartidaEquipoId
                        FOREIGN KEY (PartidaEquipoId) REFERENCES partidas_equipo(Id)
                ) CHARACTER SET utf8mb4;
                """, cancellationToken);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task<bool> ExisteColumnaAsync(DbConnection conexion, string tabla, string columna,
        CancellationToken cancellationToken)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            SELECT COUNT(*) FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tabla AND COLUMN_NAME = @columna;
            """;
        var parametroTabla = comando.CreateParameter();
        parametroTabla.ParameterName = "@tabla";
        parametroTabla.Value = tabla;
        comando.Parameters.Add(parametroTabla);
        var parametroColumna = comando.CreateParameter();
        parametroColumna.ParameterName = "@columna";
        parametroColumna.Value = columna;
        comando.Parameters.Add(parametroColumna);
        return Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private static async Task EjecutarAsync(DbConnection conexion, string sql, CancellationToken cancellationToken)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = sql;
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }
}
