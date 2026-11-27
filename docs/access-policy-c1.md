# Política de acceso y cobranza (C1 — fase 2)

**Estado:** diseño; no implementado.  
**Fecha:** 2026-11-27.  
**Contexto:** C1 en modo aviso ya lista vencidos y próximos. Esta fase define gracia, excepciones y el bloqueo configurable.

## Punto de partida real

El sistema **ya bloquea** a quien no tiene membresía vigente (`sin membresía → sin acceso`) y no tiene gracia. Por eso C1 fase 2 no “activa un bloqueo nuevo”: agrega **tolerancia, excepciones y registro de decisiones** alrededor de una regla que ya existe.

Consecuencia: activar 3 días de gracia **suaviza** el acceso; no lo endurece. Cualquier cambio debe probarse con datos reales.

## Propuesta (sujeta a aprobación del dueño)

| Parámetro | Valor propuesto |
|---|---|
| Días de gracia | 3 naturales |
| Aviso “por vencer” | 7 días |
| Duración máxima de excepción | 3 días |
| Quién autoriza excepción | Dueño y encargado |
| Motivo | Catálogo cerrado + nota |
| Modo | `Off` (regla actual), `Shadow` (calcula sin bloquear), `Enforce` |
| Prórroga retroactiva | No; se registra desde su alta |
| Socio suspendido | Bloqueado siempre, sin excepción |

## Estados de acceso

| Estado | Condición | Acceso |
|---|---|---|
| Vigente | hoy ≤ fin | Permitido |
| Por vencer | vigente y faltan ≤ 7 días | Permitido + aviso |
| En gracia | fin < hoy ≤ fin + gracia | Permitido + aviso |
| Vencido | pasó la gracia | Bloqueado |
| Sin membresía | nunca tuvo o solo cancelada | Bloqueado |
| Excepción activa | autorización vigente | Permitido + marca |
| Suspendido | baja administrativa/seguridad | Bloqueado |

Precedencia: suspensión → excepción → membresía/gracia → sin membresía.
El crédito a favor **no** habilita acceso por sí solo; solo al aplicarse a una renovación.

## Evaluación

1. Suspensión explícita.
2. Excepción vigente.
3. Membresía vigente/gracia.
4. Sin membresía.
5. Registrar decisión y modo.

- **Fail-open:** si falla la evaluación, se permite y se registra el error. Un gimnasio sin torniquete no puede dejar a alguien afuera por un bug.
- Fechas de negocio en zona local; en BD UTC.
- La decisión en línea calcula con la hora actual; un job nocturno solo refresca reportes.

## Datos propuestos

### `GYM_ACCESS_POLICY` (una fila vigente, versionada)

`MODE` (`off`/`shadow`/`enforce`), `GRACE_DAYS`, `NOTICE_DAYS`, `EXCEPTION_MAX_DAYS`, `REQUIRE_OWNER_APPROVAL`, `UPDATED_BY`, `UPDATED_AT`, `VERSION`.

Cada cambio se guarda en `GYM_ACCESS_POLICY_HISTORY` (snapshot, usuario, motivo).

### `GYM_ACCESS_EXCEPTIONS`

`MEMBER_ID`, `FROM_AT`, `TO_AT`, `REASON_CODE`, `NOTES`, `REQUESTED_BY`, `AUTHORIZED_BY`, `STATUS` (`active`/`expired`/`revoked`), `CREATED_AT`, `REVOKED_AT`, `REVOKED_BY`.

- Duración ≤ máximo configurado.
- Motivos: `transfer_pending`, `payment_pending`, `admin_error`, `courtesy`, `medical`, `management`.
- Una excepción activa por socio y tipo; índice filtrado.
- El encargado puede autorizar hasta el máximo; el dueño puede revocar.

### `GYM_ACCESS_DECISIONS` (append-only)

`MEMBER_ID`, `DEVICE_ID`, `DECIDED_AT`, `MODE`, `CALCULATED` (`allow`/`deny`), `APPLIED` (`allow`/`deny`), `STATE`, `EXCEPTION_ID`, `REASON`, `IDEMPOTENCY_KEY`, `LATENCY_MS`, `ERROR`.

- `CALCULATED` vs `APPLIED` es lo que muestra la sombra.
- Índice único por `IDEMPOTENCY_KEY` para evitar dobles check-ins.
- Sin `UPDATE`/`DELETE`.

## Feature flag y activación

- El modo vive en BD, no en el despliegue; cambio en caliente, caché ≤ 30 s.
- **Off** = regla actual; **Shadow** = calcula y registra, no bloquea; **Enforce** = aplica.
- Rollback: volver a Off/Shadow en < 1 minuto, sin migración inversa.
- Interruptor de emergencia visible solo para dueño/admin y comando SQL documentado.

Secuencia:
1. Desplegar en **Off**; validar tablas y job.
2. **Shadow 10–14 días**; revisar “habría bloqueado” y falsos positivos.
3. Limpiar fechas/saldos y avisar a socios con 7 días de anticipación.
4. Aprobación escrita del dueño y activación gradual en horario valle.
5. Si hay incidente, volver a Shadow.

## API y UI

- `GET/PUT /api/access/policy` (dueño/admin).
- `GET/POST /api/access/exceptions`, `POST /api/access/exceptions/{id}/revoke`.
- `GET /api/access/decisions?from=&to=` para el reporte de sombra.
- UI: **Accesos → Política**, **Excepciones** y **Decisiones**; el modal de Cobranza muestra el estado calculado y permite crear excepción.
- Kiosco: mensajes claros, sin montos ni “moroso”; solo estado e instrucción de pasar a recepción.
  - Vigente: “Bienvenido”.
  - Gracia: “Tu membresía venció el X; pasa a recepción al salir”.
  - Bloqueado: “No pudimos validar tu acceso; acércate a recepción”.

## Casos borde

- **Congelada:** no consume gracia; se trata como suspendida temporal. Recepción puede reactivar.
- **Pago parcial / promesa:** excepción `payment_pending` con fecha; al vencer, vuelve a la regla.
- **Transferencia sin confirmar:** excepción `transfer_pending` de 1 día; si no se confirma, no se renueva.
- **Paga en el momento:** registrar pago y desbloquear en la misma operación; no pedir otro check-in.
- **Socio antiguo vencido:** regularización inicial de 7 días desde el aviso, sin excepción permanente.
- **Crédito a favor:** se muestra, no desbloquea.

## Pruebas mínimas

- Tabla de decisión con fronteras: último día, día +1, fin de gracia, medianoche local, cambio de zona.
- Excepción activa/expirada/revocada/agotada; precedencia con suspensión.
- Pago que cubre, pago anulado, congelamiento, reanudación.
- Idempotencia: dos kioscos, doble toque, reintento.
- Fail-open con BD caída.
- Cambio de modo en caliente y rollback < 1 min.
- Shadow: comparar 30 días de decisiones contra la regla actual.
- Aceptación para Enforce: ≥10 días en sombra, 0 falsos positivos sin explicar, p95 < 300 ms, rollback probado, recepción capacitada y aprobación del dueño.

## Decisión pendiente

El dueño debe confirmar: **3 días de gracia**, **excepción máxima de 3 días**, **dueño/encargado autorizan** y **modo inicial Off**. Sin esa confirmación no se implementa el bloqueo.
