@echo off
setlocal
pushd "%~dp0" || exit /b 1
rem Cliente para otros equipos: escribe la direccion del servidor en la ventana de acceso.
dotnet run --project "JuegoAviones.csproj"
if errorlevel 1 (
    echo.
    echo No se pudo ejecutar el cliente. Verifica el SDK de .NET 10.
    pause
)
popd
