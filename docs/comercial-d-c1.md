# Paquete comercial — Membresías y Cobranza

**Estado:** borrador para validar con el dueño; no es una cotización cerrada.  
**Fecha:** 2026-11-27.  
**Base:** POSale ya tiene precio de referencia $5,900 / $7,900 + soporte $350–$450/mes. Este documento define cómo se empaqueta D + C1 en Centro Fit.

## Principio

- **No se cobra por ver quién debe.** El historial y la lista de vencidos son información básica del negocio.
- Se cobra por **automatización y control**: bloqueo configurable, excepciones, alertas, reportes de recuperación y conciliación.
- El paquete debe venderse como “recuperar mensualidades sin perder socios”, no como “bloquear la puerta”.

## Paquetes

| Paquete | Incluye | Precio propuesto |
|---|---|---|
| **Base — Administración** | Socios, planes, membresías, historial D, pagos, caja, reportes base, respaldos y auditoría | Incluido en la licencia del producto |
| **Cobranza y control (C1)** | Lista de vencidos/próximos, avisos, gracia configurable, bloqueo opcional, excepciones auditadas y reporte de recuperación | **$1,500–$2,500 MXN** único **o** +$200–$300/mes; decidir después del piloto |
| **Cobranza automática (C2)** | Pasarela, cargo recurrente, conciliación, webhooks y soporte de contracargos | Cotización por implementación + comisión del proveedor |
| **Instalación y capacitación** | Configuración, catálogo, usuarios, respaldo y capacitación de recepción | $1,000–$1,500 MXN |
| **Soporte y actualizaciones** | WhatsApp en horario laboral, respuesta ≤1 día hábil, actualizaciones de la versión | $350–$450 MXN/mes |

> Los rangos son una hipótesis. No cerrar precio de C1 antes de medir cuánto recupera el gimnasio en 30 días.

## Qué incluye C1

- Vista **Cobranza** con vencidos, próximos 7 días, sin membresía y crédito a favor.
- Exportación CSV/Excel.
- Configuración de gracia (propuesta: 3 días) y aviso previo (7 días).
- Modos **Off / Shadow / Enforce** con interruptor de emergencia.
- Excepciones de hasta 3 días, con motivo, autorizador y auditoría.
- Reporte de recuperación: renovaron después del aviso, excepciones usadas, falsos bloqueos.
- Capacitación de recepción (30–45 minutos) y guion de mostrador.

## Qué NO incluye

- CFDI/timbrado fiscal.
- Pasarela o cargo automático (C2).
- App móvil, nube, multitienda o sincronización.
- Torniquetes, biometría o hardware de acceso.
- Soporte 24/7.

## Discurso de venta (30 segundos)

> “Centro Fit ya te dice quién está por vencer y quién debe. Con Cobranza puedes avisar antes, dar unos días de gracia y decidir tú si bloqueas o no. Todo queda auditado, y recepción puede resolver una excepción en segundos. La meta no es cerrar la puerta: es que el socio renueve antes de llegar al bloqueo.”

## Demostración

1. **Miembros → Cobranza**: mostrar conteos, filtros y exportación.
2. Abrir un socio vencido y uno próximo; explicar la diferencia.
3. Mostrar el modo **Shadow**: “esto habría bloqueado hoy”, sin bloquear.
4. Crear una excepción de 1 día con motivo y mostrar la auditoría.
5. Enseñar el mensaje de kiosco y el guion de recepción.
6. Cerrar con la métrica: renovaciones recuperadas y falsos bloqueos.

## Contrato — borrador de cláusulas

> Revisar con un abogado antes de firmar. No es asesoría legal.

1. **Licencia:** perpetua, no exclusiva, por un negocio y una sucursal; un servidor; usuarios según el paquete. No se permite revender ni sublicenciar.
2. **Alcance:** módulos contratados; lo no listado queda fuera. C1 se activa solo después de la fase sombra y aprobación escrita.
3. **Entrega e instalación:** instalación en el servidor del cliente, configuración inicial, catálogo y capacitación. El cliente proporciona hardware, Windows, SQL Server y red local.
4. **Datos:** son del cliente. El proveedor puede acceder solo con autorización para soporte; el paquete de soporte no incluye la base sin consentimiento.
5. **Respaldos:** el sistema genera respaldos, pero la custodia externa (USB/OneDrive) es responsabilidad del cliente. El proveedor ayuda a configurarla.
6. **Soporte:** horario laboral, canal acordado, respuesta objetivo ≤1 día hábil. No cubre hardware, red, SQL Server, CFDI ni desarrollos nuevos.
7. **Actualizaciones:** las versiones de la rama contratada se entregan durante el periodo de soporte. Cambios de alcance se cotizan.
8. **Precio y pago:** licencia única + soporte mensual/anual; instalación aparte. La falta de pago suspende soporte y actualizaciones, no borra datos.
9. **Aceptación:** el cliente valida instalación, accesos, cobros y respaldo en una ventana acordada. Los datos de prueba se eliminan.
10. **Garantía:** 30 días para defectos reproducibles del software; no cubre mal uso, cambios de terceros ni hardware.
11. **Límite de responsabilidad:** hasta el importe pagado por la licencia del año; no cubre lucro cesante ni pérdida de datos por fallos de hardware/red.
12. **Terminación:** el cliente conserva su base y puede exportarla; el proveedor entrega un respaldo final si está al corriente.
13. **Confidencialidad y privacidad:** ambas partes protegen datos de socios; el proveedor no los usa para otro fin.
14. **CFDI:** el producto no timbra; si se requiere, se cotiza como extensión con un PAC.
15. **Piloto:** 30 días, alcance acordado, métricas de recuperación y falsos bloqueos; al cierre se decide continuar.

## Métricas del piloto

- Socios vencidos que renuevan en 30 días.
- Porcentaje de accesos bloqueados que eran falsos positivos.
- Excepciones por socio/mes y quién las autoriza.
- Tiempo de recepción para renovar (<45 segundos).
- Satisfacción del dueño y de recepción.

## Siguiente decisión

Definir si C1 se vende como **pago único** o **mensualidad**, y si el piloto incluye C1 gratis a cambio de métricas y testimonio. No cerrar precio antes de la fase sombra.
