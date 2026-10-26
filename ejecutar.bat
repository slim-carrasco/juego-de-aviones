@echo off
setlocal
pushd "%~dp0" || exit /b 1
curl.exe -fsS --max-time 2 http://127.0.0.1:5080/health >nul 2>&1
if errorlevel 1 (
    echo Iniciando servidor del juego...
    start "Servidor JuegoAviones" cmd /c call "%~dp0iniciar-servidor.bat"
    for /l %%i in (1,1,45) do (
        curl.exe -fsS --max-time 2 http://127.0.0.1:5080/health >nul 2>&1
        if not errorlevel 1 goto servidor_listo
        timeout /t 1 /nobreak >nul
    )
    echo.
    echo No se pudo conectar con el servidor. Verifica MySQL y revisa la consola del servidor.
    pause
    popd
    exit /b 1
)
:servidor_listo
dotnet run --project "JuegoAviones.csproj"
if errorlevel 1 (
    echo.
    echo No se pudo ejecutar el juego. Verifica que el SDK de .NET 10 este instalado.
    pause
)
popd
