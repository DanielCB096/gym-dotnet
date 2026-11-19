# Reglas de negocio (portadas del proyecto original)

> Fuente revisada: `C:\proyectos\gimnasio` (`CLAUDE.md`, `docs/02-modulos/membresias-y-cobro-recurrente.md`,
> `docs/02-modulos/control-de-acceso.md`, `docs/03-procesos/alta-de-socio-y-primer-pago.md`,
> `docs/03-procesos/congelamiento-y-cancelacion.md`, `docs/03-procesos/renovacion-y-cobro-recurrente.md`
> y ADR-087). Se aplican al backend .NET de este repositorio.

## Principios generales

- Dinero con decimal exacto (`decimal`), nunca `float`.
- Fechas de negocio en fecha local del gimnasio; la auditoría usa la fecha del servidor.
- La lógica de negocio vive en los módulos (`GymService/Modules`), no en los controladores.
- Los permisos se validan siempre en backend; el frontend solo oculta acciones.
- Los históricos no se borran: se archivan, desactivan o marcan con estado.
- Los cambios sensibles quedan auditados (`CREATED_BY`, `LAST_UPDATED_BY`, logs).
- Nunca se guardan datos de tarjeta (PAN/CVV); solo referencias o cobros declarados.
- Los cobros deben ser idempotentes cuando exista una pasarela. En esta entrega los
  cobros son manuales y presenciales.

## Membresías y socios

1. El estado de la **cuenta** (`GYM_MEMBERS.STATUS`) y el de la **membresía**
   (`GYM_MEMBERSHIPS.STATUS`) son conceptos separados.
2. Un socio sin membresía vigente puede existir, pero **no tiene acceso** al gimnasio.
   En el panel se muestra “Sin membresía”, no “Activo”.
3. Una membresía solo está activa si su periodo incluye la fecha actual y no está
   congelada, cancelada o vencida.
4. **No hay periodo de gracia:** el acceso se bloquea en la fecha de vencimiento.
5. Una **renovación** extiende desde `max(hoy, vencimiento actual)` para no perder
   días ya pagados. Una renovación anticipada suma el nuevo periodo al final.
6. Una membresía **congelada** no se renueva ni permite acceso hasta reanudarla.
7. Se puede asignar o renovar **sin cobro** (cortesía) de forma explícita y auditada;
   en ese caso no se crea un pago.
8. El cobro manual acepta `cash`, `card` (declarada) o `transfer`, registra el pago
   y activa/renueva la membresía en la misma operación.
9. Un socio no debe tener dos membresías activas simultáneas; la operación de
   renovación actualiza la vigente o crea una nueva si ya venció/canceló.
10. Un socio sin plan al darse de alta queda como “sin membresía” hasta que se le
    asigne uno. Los planes base (Mensual/Trimestral/Anual) se crean si no existen.

## Control de acceso

1. Solo entra quien tiene cuenta activa **y** membresía activa y vigente.
2. Membresía congelada, vencida o inexistente ⇒ acceso denegado.
3. Se aplica el **límite diario de entradas** del plan (`MAX_DAILY_ENTRIES`);
   `0` o negativo significa sin límite.
4. Cada código es de un solo uso; los códigos vencidos o usados no sirven.
5. La validación de códigos tiene rate limiting estricto.
6. Todo intento denegado se registra en la bitácora (`ACCESS_RESULT = 'denied'`).
7. La validación se repite al usar el código, aunque se haya generado antes:
   una membresía que vence entre la emisión y el uso no concede acceso.

## Estado en esta implementación

| Regla | Estado |
|---|---|
| Estado de membresía separado del estado de la cuenta | Implementado |
| Sin membresía ⇒ sin acceso | Implementado |
| Sin periodo de gracia | Implementado |
| Renovación desde `max(hoy, vencimiento)` | Implementado |
| Congelada no renueva ni accede | Implementado |
| Asignación/renovación sin cobro auditada | Implementado |
| Cobro manual (`cash`/`card`/`transfer`) y pago registrado | Implementado |
| Límite diario de entradas por plan | Implementado |
| Código de un solo uso + rate limiting | Implementado |
| Bitácora de accesos denegados | Implementado |
| Pasarela de pago, tokenización y renovación automática real | Pendiente (fuera de esta entrega) |
| Bloqueo por adeudo, horarios por plan, aforo y antipassback | Pendiente |
| Eventos/historial de membresía dedicados | Pendiente (se conservan pagos y auditoría) |
| Cancelación con motivo y prorrateo | Pendiente |
