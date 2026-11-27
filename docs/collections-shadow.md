# Cobranza en modo aviso (C1)

**Estado:** implementado en `feat/collections-shadow` (2026-11-27); pendiente prueba con SQL Server.  
**Regla:** esta versión **no bloquea accesos**. Solo lista vencidos, próximos y sin membresía para que el dueño valide los datos antes de activar el bloqueo duro.

## Alcance

- `GET /api/members/collections?days=7` (permiso `members.view`).
- Devuelve:
  - resumen: vencidas, próximas dentro de N días, sin membresía y crédito a favor;
  - socios activos con membresía vencida o próxima a vencer;
  - plan, vencimiento, días restantes, último pago y crédito.
- En el panel: botón **Cobranza** en Miembros → modal con resumen, filtros y tabla.
- No modifica membresías, accesos, pagos ni caja.

## Consulta

La consulta usa la última membresía no cancelada del socio (`active`, `frozen` o anterior), el plan y el último pago. Un socio sin membresía aparece como `sin_membresia`.

## Qué NO hace todavía

- No activa bloqueo por adeudo.
- No aplica periodo de gracia.
- No registra excepciones ni motivos.
- No calcula un saldo deudor: en este modelo el pago es por adelantado; “vencida” significa que la membresía terminó.

## Prueba en casa

1. Abrir **Miembros → Cobranza** con datos reales o una copia.
2. Verificar que los conteos coinciden con la base:
   ```sql
   SELECT COUNT(*) FROM gym_members m
   WHERE m.status = 'active'
     AND NOT EXISTS (SELECT 1 FROM gym_memberships mm
                     WHERE mm.member_id = m.id AND mm.status = 'active'
                       AND mm.end_date >= CAST(GETDATE() AS date));
   ```
3. Revisar un socio vencido, uno próximo y uno sin membresía.
4. Confirmar que **ningún acceso cambió**: un socio vencido sigue con la regla actual (sin acceso) y uno activo entra normal.
5. Validar la lista con el dueño y decidir periodo de gracia/excepciones antes de activar el bloqueo.

## Siguiente

- Semana 1–2 en sombra: revisar diariamente vencidos y próximos.
- Ajustar consulta/UX con el dueño.
- Solo entonces implementar bloqueo configurable, gracia y excepciones auditadas.
