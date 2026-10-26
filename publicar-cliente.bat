@echo off
setlocal
pushd "%~dp0" || exit /b 1
dotnet publish "JuegoAviones.csproj" -c Release -r win-x64 --self-contained true -o "dist\cliente-windows"
if errorlevel 1 (
    echo.
    echo No se pudo publicar el cliente.
    pause
    popd
    exit /b 1
)
echo Cliente listo en dist\cliente-windows
popd
