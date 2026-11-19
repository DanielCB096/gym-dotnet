# Pruebas en vivo (obligatorias)

> Regla vigente desde **2026-11-19**. Aplica a todo cambio funcional de este repositorio.

## Regla

Ninguna funcionalidad se considera terminada solo porque compile o porque pasen las
pruebas automatizadas. Antes de cerrar un cambio se debe probar **en vivo**:

1. **Backend/API:** contra el servicio real (no solo mocks), verificando respuesta,
   código HTTP, datos persistidos y validaciones de error.
2. **UI/navegador:** recorrer la pantalla real, abrir modales, guardar y comprobar el
   resultado; revisar que no haya errores de consola ni pantallas vacías.
3. **Permisos:** probar el rol afectado (por ejemplo STAFF/CASHIER) y confirmar 403
   donde corresponda.
4. **Datos:** usar una base temporal o datos de prueba autorizados para acciones
   destructivas. No borrar ni modificar datos reales sin autorización explícita.

## Evidencia mínima

- Comandos ejecutados y resultado (build, pruebas, smoke HTTP).
- Pantalla o flujo probado en el navegador.
- Hallazgos corregidos y cualquier limitación que impidió probar algo.

## Excepción

Si no se puede probar en vivo (sin servicio, sin DB, sin navegador), el cambio queda
**incompleto** hasta validarlo. Se debe indicar claramente qué falta y no presentarlo
como terminado.

## Historial

- 2026-11-19: se establece la regla tras detectar en pruebas de navegador dos fallos
  que las pruebas automatizadas no cubrían: el popup del recibo quedaba bloqueado y
  la ruta `/kiosk` de la PWA ocultaba la página administrativa de Kioscos.
