@echo off
setlocal
pushd "%~dp0" || exit /b 1
rem La VM accede al puerto publicado por Docker en la puerta de enlace del host.
set "ConnectionStrings__Juego=Server=172.17.0.1;Port=3307;Database=juegoaviones;User=juego;Password=juego_dev"
dotnet run --project "JuegoAviones.Servidor\JuegoAviones.Servidor.csproj" --urls http://0.0.0.0:5080
if errorlevel 1 (
    echo.
    echo No se pudo iniciar el servidor. Comprueba MySQL y el SDK de .NET 10.
    pause
)
popd
