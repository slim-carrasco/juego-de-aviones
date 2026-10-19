@echo off
setlocal
cd /d "%~dp0"
dotnet run --project "JuegoAviones.csproj"
if errorlevel 1 (
    echo.
    echo No se pudo ejecutar el juego. Verifica que el SDK de .NET 10 este instalado.
    pause
)
