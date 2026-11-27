# Historial de membresía (D) — diseño

**Estado:** diseño aprobado para implementar; sin código todavía.  
**Fecha:** 2026-11-27.  
**Objetivo:** conservar y mostrar todos los cambios de membresía de un socio, sin reconstruir historia que nunca se registró.

## Situación actual

- `gym_memberships` guarda el periodo vigente y el último estado; una renovación **actualiza** `end_date` y se pierde el vencimiento anterior.
- `GetMembershipHistory` devuelve las filas de membresía, pero no renovaciones, congelamientos, reanudaciones ni el pago ligado.
- `gym_payments` conserva pagos, pero no el periodo que compraron.
- `gym_memberships` tiene `created_at`, `updated_at`, `cancelled_at`, `cancel_reason`, `freeze_start` y `freeze_end`, útiles para un backfill limitado.

## Decisión

Crear una tabla inmutable de eventos, escribirla **hacia adelante** en la misma transacción que la operación y mostrar una línea de tiempo. El backfill histórico es una fase separada y se marca como reconstruido.

### Tabla `gym_membership_events`

| Columna | Tipo | Notas |
|---|---|---|
| `id` | BIGINT | PK; secuencia propia |
| `member_id` | BIGINT | FK a `gym_members` |
| `membership_id` | BIGINT NULL | FK a `gym_memberships` |
| `event_type` | VARCHAR(30) | `created`, `renewed`, `plan_changed`, `frozen`, `resumed`, `cancelled`, `payment`, `courtesy`, `credit_applied`, `backfill` |
| `event_at` | DATETIME2 | Fecha real del evento |
| `plan_id` | BIGINT NULL | Plan del evento |
| `previous_end_date` | DATE NULL | Antes del cambio |
| `new_end_date` | DATE NULL | Después del cambio |
| `amount` | DECIMAL(10,2) NULL | Cobro o crédito |
| `currency` | VARCHAR(3) NULL | Moneda |
| `payment_id` | BIGINT NULL | Pago relacionado |
| `reason` | VARCHAR(300) NULL | Cancelación, congelamiento o nota |
| `user_id` | BIGINT NULL | Usuario que ejecutó |
| `source` | VARCHAR(20) | `live` o `backfill` |
| `details` | VARCHAR(MAX) NULL | JSON acotado para datos extra |

Índices por `member_id`, `membership_id`, `event_at` y `event_type`. Sin `UPDATE` ni `DELETE` desde la aplicación; la inmutabilidad se refuerza con triggers, como en el resto del sistema.

### Cuándo se escribe

- `AssignMembership`: `renewed` si actualiza una membresía activa; `created` si inserta una nueva. Guarda `previous_end_date`, `new_end_date`, plan, importe pagado, crédito aplicado y `payment_id`.
- `CancelMembership`: `cancelled` con motivo, fecha y crédito generado.
- `FreezeMembership`: `frozen` con rango y motivo.
- `ResumeMembership`: `resumed` con el nuevo vencimiento si cambió.
- `RecordPayment`: `payment` cuando el pago no venga de una renovación (para no duplicar).
- Cortesía: `courtesy` en lugar de `payment`.
- Todo en la misma sesión/transacción que la operación; si falla el evento, no se confirma el cambio.

### Backfill (fase 2, no bloqueante)

- Un evento `created` por cada fila de `gym_memberships` con `source=backfill`.
- `cancelled` si `cancelled_at` no es nulo; `frozen` si hay rango de congelamiento.
- Un `payment` por cada fila de `gym_payments` con `membership_id`.
- **No inventar renovaciones intermedias.** Si una membresía se renovó varias veces antes de esta versión, se muestra un solo periodo con una nota de “historial reconstruido”.
- Reporte de calidad: socios sin historial coherente, pagos sin membresía, membresías sin plan.
- `source=backfill` nunca se usa para bloqueos de C1; solo para consulta.

### API

`GET /api/members/{memberId}/history` devuelve:

```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "type": "renewed",
      "at": "2026-11-27T18:00:00",
      "planName": "Mensual",
      "previousEndDate": "2026-11-30",
      "newEndDate": "2026-12-30",
      "amount": 450.00,
      "currency": "MXN",
      "paymentId": 123,
      "reason": "",
      "source": "live",
      "userName": "Recepción"
    }
  ]
}
```

Permiso: `members.view`. El endpoint viejo `/memberships` se conserva para no romper el frontend.

### UI

- En la ficha/modal del socio, pestaña **Historial** con línea de tiempo, filtros por tipo y fecha.
- Cada renglón: icono, tipo en español, plan, periodo anterior → nuevo, importe, usuario y motivo.
- Los pagos enlazan al recibo existente.
- Los eventos `backfill` llevan distintivo **Reconstruido** y una nota.
- Estado vacío: “Aún no hay movimientos registrados”.

### Pruebas

- Integración: crear, renovar, congelar, reanudar y cancelar; verificar eventos, orden y que el fallo de un evento revierta la operación.
- Backfill: conteos y marcas; no debe crear renovaciones inventadas.
- UI: línea de tiempo, filtros, recibo y estado vacío.
- En vivo: un socio de prueba con al menos dos renovaciones, un congelamiento y una cancelación; revisar en navegador y borrar los datos temporales.

## Fuera de alcance

- Bloqueo por adeudo (C1) y pasarela (C2).
- Historial de cambios de datos personales; eso ya vive en la auditoría.
- Reportes históricos de ingresos por periodo; se pueden apoyar en esta tabla después.

## Orden de implementación

1. Tabla + entidad + migración aditiva (con respaldo y prueba en copia).
2. Escritura de eventos en las cinco operaciones.
3. Endpoint y UI de línea de tiempo.
4. Backfill marcado y reporte de calidad.
5. Prueba en vivo y despliegue junto con la siguiente versión.
