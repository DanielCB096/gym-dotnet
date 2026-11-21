# Kiosco en este PC

Kiosco local para una segunda pantalla con teclado, independiente del tablet Android.

## Dispositivo

- Nombre: **Kiosco PC**
- Registrado en el panel/API como kiosco activo.
- Token guardado en el perfil dedicado de Edge:
  `%LOCALAPPDATA%\GymServiceKiosk\profile`
- El token no se guarda en el repositorio. Se rota desde **Panel → Kioscos** si se
  pierde o se compromete.

## Lanzador

`Start-Kiosk.ps1`:

- Detecta la **segunda pantalla** y abre Edge en modo kiosco en esa pantalla.
- Si no hay segunda pantalla, no hace nada.
- `-ForcePrimary` permite probar en la pantalla principal.
- `-Windowed` abre una ventana de 1280×800 para pruebas, sin pantalla completa.

El teclado físico funciona con:

- Dígitos `0–9`
- `Backspace` para borrar el último dígito
- `Enter` para validar
- `Esc` para limpiar el código

## Accesos directos

- Escritorio: **Kiosco segunda pantalla** (solo si hay segunda pantalla).
- Escritorio: **Kiosco ventana (prueba)** (pantalla principal, ventana).
- Inicio: **Kiosco segunda pantalla** (se ejecuta al iniciar sesión; si no hay
  segunda pantalla, termina sin abrir nada).

## Requisitos

- El PC ya confía en la CA local (`GymService LAN Root CA`) en el almacén
  `LocalMachine\Root`.
- El servicio `centrofitgymservice` debe estar corriendo.
- Edge instalado (se usa un perfil dedicado, separado del navegador personal).

## Reconfigurar el token

1. Rotar el token en **Panel → Kioscos → Kiosco PC → Rotar token**.
2. Abrir el perfil dedicado y entrar a `https://192.168.100.53:5055/kiosk-app/`.
3. En **Configurar dispositivo**, poner la URL y el token nuevos y guardar.

También se puede borrar `%LOCALAPPDATA%\GymServiceKiosk\profile` y volver a
configurar el perfil desde cero.
