# Red local temporal para jugar entre laptops

Esta guía registra la conexión usada durante la prueba del 3 vs 3. El código y MySQL quedaron en Omarchy; el servidor del juego se ejecutó en la VM Windows. Las otras laptops solo necesitaron `JuegoAviones.exe` y acceso a una red local común.

## Conexión que se usó

```text
Laptops Windows ── Wi-Fi Aeronaves-Joel ── Omarchy 192.168.50.1:5080
                                              │ puente TCP temporal (socat)
                                              └── VM Windows:5080 ── MySQL en Docker
```

Omarchy creó el punto de acceso `Aeronaves-Joel` con NetworkManager en la interfaz virtual `ap0` y asignó direcciones `192.168.50.x` por DHCP. El puente reenviaba únicamente el puerto 5080 a la VM. En cada cliente, el campo **Servidor** se configuró como `http://192.168.50.1:5080`; `localhost` en otra laptop apunta a esa misma laptop. Se comprobó la ruta abriendo `http://192.168.50.1:5080/health` desde una laptop y obteniendo `{"estado":"ok"}`.

El teléfono proporcionaba internet a Omarchy por otra conexión Wi-Fi. Las laptops no necesitaban internet para jugar: debían permanecer conectadas a la red local. Con una sola tarjeta Wi-Fi, el punto de acceso y la conexión al teléfono pueden requerir el mismo canal y resultar inestables. Un router o un segundo adaptador Wi-Fi simplifica esta parte.

## Repetir la prueba en otra PC con Omarchy y VM Windows

1. En Omarchy, iniciar la VM Windows, MySQL y `iniciar-servidor.bat`. Dentro de Windows, comprobar `http://localhost:5080/health`. La VM debe tener una IP alcanzable desde Omarchy. Buscarla con `docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}} {{end}}' omarchy-windows` si se usa ese contenedor; puede cambiar al reiniciar.
2. Usar una red local privada. La opción más simple es conectar Omarchy y las laptops al mismo router. Si se necesita crear el punto de acceso desde Omarchy, verificar que el adaptador permita modo AP con `iw list`. En una tarjeta compartida con otra conexión Wi-Fi, consultar el canal actual con `iw dev` y usar ese canal para el AP.
3. Para recrear el punto de acceso de esta prueba, sustituir `<INTERFAZ_WIFI>`, `<CANAL>` y `<CLAVE_WIFI>` en los comandos siguientes. Elegir una clave propia y no guardarla en el repositorio:

   ```bash
   sudo iw dev <INTERFAZ_WIFI> interface add ap0 type __ap
   nmcli connection add type wifi ifname ap0 con-name Aeronaves-Joel-Local ssid Aeronaves-Joel
   nmcli connection modify Aeronaves-Joel-Local 802-11-wireless.mode ap 802-11-wireless.band bg 802-11-wireless.channel <CANAL> wifi-sec.key-mgmt wpa-psk wifi-sec.psk '<CLAVE_WIFI>' ipv4.method shared ipv4.addresses 192.168.50.1/24 ipv6.method disabled connection.autoconnect no
   nmcli connection up Aeronaves-Joel-Local ifname ap0
   ```

   En otra PC pueden variar los nombres de interfaz y el soporte de AP. Si ya hay un router, omitir estos comandos y usar la IP local que ese router asigne a Omarchy.
4. Permitir tráfico de la red elegida al puerto 5080 y, para el AP creado por Omarchy, DHCP. Estas fueron las reglas UFW de la prueba:

   ```bash
   sudo ufw allow in on ap0 from 192.168.50.0/24 to 192.168.50.1 port 5080 proto tcp
   sudo ufw allow in on ap0 to any port 67 proto udp
   ```

   Esta PC tenía además una cadena `nftables` que descartaba paquetes antes de UFW. Solo si la otra PC tiene esa misma cadena, se requieren estas excepciones temporales:

   ```bash
   sudo nft insert rule inet local_access input iifname "ap0" udp dport 67 accept
   sudo nft insert rule inet local_access input iifname "ap0" ip saddr 192.168.50.0/24 ip daddr 192.168.50.1 tcp dport 5080 accept
   ```

   Revisar la configuración real del firewall antes de aplicarlas. Una PC que solo use UFW no necesita estos comandos.
5. Desde Omarchy, iniciar el puente TCP con la IP actual de la VM; mantener esta terminal abierta durante la prueba:

   ```bash
   socat TCP-LISTEN:5080,bind=192.168.50.1,reuseaddr,fork TCP:<IP_VM>:5080
   ```

   Con un router, reemplazar `192.168.50.1` por la IP de Omarchy en ese router y ajustar la regla del firewall a esa interfaz y subred. Si el servidor corre directamente en Omarchy, no hace falta el puente.
6. En las laptops, conectarse a la misma red, abrir `http://192.168.50.1:5080/health` y configurar **Servidor** con `http://192.168.50.1:5080`. Usar cuentas distintas. Si el navegador responde pero el juego no, revisar que el campo **Servidor** no siga en `localhost`.

## Cerrar el acceso al terminar

Detener `socat` con Ctrl+C, retirar las reglas del firewall y apagar el punto de acceso:

```bash
sudo ufw delete allow in on ap0 from 192.168.50.0/24 to 192.168.50.1 port 5080 proto tcp
sudo ufw delete allow in on ap0 to any port 67 proto udp
nmcli connection down Aeronaves-Joel-Local
nmcli connection delete Aeronaves-Joel-Local
sudo iw dev ap0 del
```

Si se añadieron reglas a `inet local_access input`, obtener sus identificadores con `sudo nft -a list chain inet local_access input` y eliminar cada regla temporal con `sudo nft delete rule inet local_access input handle <IDENTIFICADOR>`. No borrar la tabla completa: contiene otras reglas del sistema.

En esta PC, el cierre se hizo el 26 de octubre de 2026: el puente y la VM ya estaban apagados, se eliminaron las reglas UFW para `ap0` y el perfil `Aeronaves-Joel-Local`. También se confirmó que no había reglas temporales de AP en `inet local_access` ni un proceso escuchando en el puerto 5080. Esta red no se reactivará automáticamente.
