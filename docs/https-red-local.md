# HTTPS en red local (acceso por IP)

> Despliegue actual: `https://192.168.100.53:5055` con un certificado emitido por la CA
> local `GymService LAN Root CA`. No hay dominio ni proxy inverso.

## Por qué TLS directo en Kestrel

- El rate limiting, los bloqueos de cuenta y la auditoría usan
  `HttpContext.Connection.RemoteIpAddress`. Detrás de IIS/Nginx todos los clientes
  llegarían como `127.0.0.1`: el límite estricto de **5/min** (OTP y validación de
  kiosco) pasaría a ser global y el general de 100/min también.
- Kestrel termina TLS en el mismo puerto; no hay proceso adicional ni puerto interno.
- El certificado cubre la IP (`SAN IP`) y `127.0.0.1`, así que funciona tanto desde
  otro equipo como desde el propio servidor.

## Arquitectura y archivos

| Elemento | Ubicación / valor |
|---|---|
| URL del panel | `https://192.168.100.53:5055` |
| Certificado del servidor | `C:\ProgramData\GymService\certs\gymservice.pfx` (clave privada) |
| CA raíz para clientes | `C:\ProgramData\GymService\certs\gymservice-root-ca.cer` |
| Configuración | `Kestrel:Certificates:Default:Path` y `Password` en `appsettings.json` |
| ACL | PFX y `appsettings.json`: `SYSTEM`, Administradores y lectura para `NT SERVICE\centrofitgymservice` |
| Firewall | Regla `GymService HTTPS`, TCP 5055, remoto `192.168.100.0/24` |
| Vigencia | Certificado del servidor: 825 días. CA raíz: 10 años |

La contraseña del PFX se guarda en `appsettings.json`; por eso el archivo ya no es
legible por `BUILTIN\Usuarios`. La clave privada de la CA raíz es **no exportable** y
permanece en el almacén `LocalMachine\My` del servidor para poder renovar el certificado.

## Emisión y renovación

El script está en el repositorio y debe ejecutarse **como Administrador**:

```powershell
.\deploy\https\New-GymServerCertificate.ps1 -IpAddress 192.168.100.53
```

Hace lo siguiente:

1. Reutiliza la CA `GymService LAN Root CA` o la crea si no existe.
2. Emite un certificado para `192.168.100.53` y `127.0.0.1` con EKU de servidor.
3. Exporta el PFX con una contraseña aleatoria y lo protege con ACL.
4. Actualiza `Kestrel` y `WebServer:Urls` (`https://0.0.0.0:5055`) en `appsettings.json`.
5. Reinicia `centrofitgymservice` y comprueba `/health` por HTTPS.

Para cambiar de IP o renovar antes del vencimiento, vuelve a ejecutar el script con la IP
nueva. La CA raíz no cambia, así que los equipos cliente no necesitan volver a confiar en
una CA distinta. Si el servidor se reinstala o se pierde la CA, hay que generar una nueva y
reinstalarla en todos los clientes.

### Paquete para clientes

El repositorio incluye `deploy/https/Build-ClientPackage.ps1`, que genera un ZIP con:

- `CentroFit-CA.cer` (solo la parte pública de la CA)
- `Instalar-CA-Windows.cmd` y `Instalar-CA-Windows.ps1`
- `LEEME.txt` con los pasos para Windows, Android, iOS, Firefox y kiosco

```powershell
.\deploy\https\Build-ClientPackage.ps1
# Genera CentroFit-CA.zip en el Escritorio
```

El paquete no incluye el PFX ni la clave privada. En este servidor se generó en
`C:\Users\PC\Desktop\CentroFit-CA.zip` y el instalador de Windows se probó desde el ZIP.

## Instalar la CA raíz en los clientes

Sin este paso el navegador muestra una advertencia de certificado
(`ERR_CERT_AUTHORITY_INVALID`). El archivo a distribuir es
`gymservice-root-ca.cer` (solo contiene la parte pública).

### Windows (Chrome, Edge, panel de escritorio)

1. Copia `gymservice-root-ca.cer` al equipo.
2. Doble clic → **Instalar certificado** → **Equipo local** → **Siguiente**.
3. **Colocar todos los certificados en el siguiente almacén** → **Entidades de
   certificación raíz de confianza** → **Siguiente** → **Finalizar**.
4. Cierra y abre el navegador.

También se puede automatizar en PowerShell (Administrador):

```powershell
Import-Certificate -FilePath .\gymservice-root-ca.cer -CertStoreLocation Cert:\LocalMachine\Root
```

### Android

1. Copia el `.cer` al dispositivo (USB, correo o descarga).
2. **Ajustes → Seguridad → Cifrado y credenciales → Instalar un certificado → Certificado de CA**.
3. Selecciona el archivo y confirma el aviso del sistema (es normal para CA de usuario).

> Android exige un **bloqueo de pantalla** (PIN/patrón) para instalar una CA de usuario.
> En HyperOS/MIUI puedes buscar “certificado” en Ajustes para llegar a “Instalar un certificado”.
> Verificado en Android 16: el panel y el kiosco cargan por HTTPS sin advertencia tras instalar
> la CA en **Credenciales de confianza → Usuario**.

### iOS / iPadOS

1. Envía el `.cer` por AirDrop o correo y ábrelo.
2. **Ajustes → Perfil descargado → Instalar**.
3. **Ajustes → General → Información → Ajustes de confianza de certificados** y activa
   la confianza total para `GymService LAN Root CA`.

### Firefox

Firefox usa su propio almacén. Activa `security.enterprise_roots.enabled = true` en
`about:config` para que use el almacén de Windows, o importa el `.cer` en
**Ajustes → Privacidad y seguridad → Certificados → Ver certificados → Autoridades**.

## Kiosco

- Instala la CA raíz en el dispositivo del kiosco.
- Abre el kiosco desde `https://192.168.100.53:5055/kiosk-app/`.
- En la configuración del kiosco, cambia `apiUrl` a `https://192.168.100.53:5055`.
  Si queda en `http://...`, las llamadas fallarán porque el puerto ya solo habla TLS.
- No abras el kiosco como archivo local (`file://`): el origen sería `null` y el navegador
  bloquearía las peticiones.

## Firewall y alcance

La regla actual permite TCP 5055 **solo** desde `192.168.100.0/24`:

```powershell
Get-NetFirewallRule -DisplayName 'GymService HTTPS' |
    Get-NetFirewallPortFilter
Get-NetFirewallRule -DisplayName 'GymService HTTPS' |
    Get-NetFirewallAddressFilter
```

- Kestrel escucha en `0.0.0.0:5055`, pero el firewall limita el origen a la LAN.
- Tailscale (`100.64.0.0/10`) y WSL (`172.17.0.0/16`) quedan bloqueados por defecto.
  Si se necesita acceso remoto, añade el rango de Tailscale a `RemoteAddress` y documenta
  el cambio; no abras el puerto a `Any`.
- Si el router no reenvía el puerto, el panel no es alcanzable desde Internet. No se
  recomienda reenviarlo: para acceso remoto es preferible Tailscale o una VPN.

## Diagnóstico rápido

```powershell
# Puerto y proceso
Get-NetTCPConnection -LocalPort 5055 -State Listen

# Salud por HTTPS (el servidor ya confía en su CA)
Invoke-WebRequest https://192.168.100.53:5055/health -UseBasicParsing

# Certificado que sirve Kestrel
$tcp = [Net.Sockets.TcpClient]::new('192.168.100.53', 5055)
$ssl = [Net.Security.SslStream]::new($tcp.GetStream(), $false, { param($s,$c,$ch,$e) $true })
$ssl.AuthenticateAsClient('192.168.100.53')
[Security.Cryptography.X509Certificates.X509Certificate2]::new($ssl.RemoteCertificate) |
    Select-Object Subject, Issuer, NotAfter
$ssl.Close(); $tcp.Close()
```

| Síntoma | Causa probable |
|---|---|
| `ERR_CERT_AUTHORITY_INVALID` | Falta instalar la CA raíz en ese dispositivo. |
| `ERR_CERT_COMMON_NAME_INVALID` | Se entra por otra IP/nombre distinto al SAN; reemite con esa IP. |
| `ERR_CONNECTION_RESET` al usar `http://` | Correcto: el puerto 5055 solo acepta TLS. Usa `https://`. |
| El servicio no arranca tras cambiar la config | Revisa el log y que el PFX exista y sea legible por `SYSTEM`. |
| El kiosco no valida códigos | `apiUrl` en `http://` o CA no instalada en el kiosco. |

## Notas de seguridad

- `appsettings.json` contiene la contraseña de SQL y la del PFX; su ACL debe seguir siendo
  `SYSTEM` + `Administradores`. No lo copies fuera del servidor.
- El PFX y la CA raíz viven en `C:\ProgramData\GymService\certs`; los respaldos de esa
  carpeta deben tratarse como material sensible (la CA permite emitir certificados).
- Usa una **reserva DHCP** o IP estática para `192.168.100.53`; si la IP cambia, el
  certificado deja de coincidir hasta que se reemita.
- El certificado es de una CA privada: válido para esta LAN, no para Internet. Para
  publicar el panel fuera de la red, usa un dominio real con un proxy que envíe
  `X-Forwarded-For` y habilita `ForwardedHeaders` en la aplicación.
