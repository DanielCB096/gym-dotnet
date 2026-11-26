import { useRef } from 'react';
import { Printer } from 'lucide-react';
import Modal from './ui/Modal';
import Button from './ui/Button';

const escapeHtml = (value) => String(value ?? '')
  .replace(/&/g, '&amp;')
  .replace(/</g, '&lt;')
  .replace(/>/g, '&gt;')
  .replace(/"/g, '&quot;')
  .replace(/'/g, '&#39;');

const formatDate = (value) => {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return String(value);
  return date.toLocaleDateString('es-MX', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

const money = (value) => new Intl.NumberFormat('es-MX', {
  style: 'currency',
  currency: 'MXN',
  minimumFractionDigits: 2,
}).format(Number(value) || 0);

const statusLabels = {
  pending: 'Pendiente',
  paid: 'Pagada',
  cancelled: 'Cancelada',
};

function buildHtml(invoice) {
  const status = statusLabels[String(invoice.status || '').toLowerCase()] || invoice.status || '—';
  const subtotal = Number(invoice.subtotal ?? (Number(invoice.amount || 0) - Number(invoice.taxAmount || 0)));
  const tax = Number(invoice.taxAmount || 0);
  const total = Number(invoice.amount || 0);

  return `<!doctype html>
<html lang="es">
<head>
<meta charset="utf-8" />
<title>Factura ${escapeHtml(invoice.invoiceNumber)}</title>
<style>
  @page { size: A4; margin: 16mm; }
  * { box-sizing: border-box; }
  body { margin: 0; color: #111827; font-family: Arial, Helvetica, sans-serif; font-size: 12px; }
  .header { display: flex; justify-content: space-between; align-items: flex-start; border-bottom: 3px solid #465fff; padding-bottom: 14px; }
  .brand { font-size: 25px; font-weight: 800; color: #465fff; letter-spacing: -0.04em; }
  .subtitle { color: #64748b; font-size: 11px; margin-top: 3px; }
  .invoice-title { text-align: right; }
  .invoice-title h1 { margin: 0; font-size: 18px; }
  .invoice-title p { margin: 4px 0 0; color: #64748b; }
  .meta { display: grid; grid-template-columns: 1fr 1fr; gap: 10px 30px; margin: 18px 0; }
  .meta div { display: flex; flex-direction: column; gap: 2px; }
  .label { color: #64748b; font-size: 10px; text-transform: uppercase; letter-spacing: 0.04em; }
  .value { font-weight: 700; }
  table { width: 100%; border-collapse: collapse; margin-top: 8px; }
  th, td { border-bottom: 1px solid #e2e8f0; padding: 9px 8px; text-align: left; }
  th { background: #f8fafc; color: #475569; font-size: 10px; text-transform: uppercase; letter-spacing: 0.04em; }
  .number { text-align: right; white-space: nowrap; }
  .totals { margin-left: auto; width: 48%; margin-top: 12px; }
  .totals div { display: flex; justify-content: space-between; padding: 5px 8px; }
  .totals .total { border-top: 2px solid #111827; margin-top: 3px; padding-top: 8px; font-size: 16px; font-weight: 800; }
  .status { display: inline-block; padding: 3px 9px; border-radius: 999px; font-size: 10px; font-weight: 700; background: #f1f5f9; }
  .status.paid { background: #dcfce7; color: #166534; }
  .status.pending { background: #fef3c7; color: #92400e; }
  .status.cancelled { background: #fee2e2; color: #991b1b; }
  .note { margin-top: 22px; color: #64748b; font-size: 10px; line-height: 1.5; }
  .footer { margin-top: 28px; padding-top: 9px; border-top: 1px solid #e2e8f0; color: #94a3b8; font-size: 10px; display: flex; justify-content: space-between; }
  .uuid { margin-top: 14px; color: #64748b; font-size: 10px; word-break: break-all; }
</style>
</head>
<body>
  <div class="header">
    <div>
      <div class="brand">Centro Fit</div>
      <div class="subtitle">Comprobante interno de facturación</div>
    </div>
    <div class="invoice-title">
      <h1>Factura ${escapeHtml(invoice.invoiceNumber || '—')}</h1>
      <p>${formatDate(invoice.invoiceDate)}</p>
      <span class="status ${escapeHtml(String(invoice.status || '').toLowerCase())}">${escapeHtml(status)}</span>
    </div>
  </div>

  <div class="meta">
    <div><span class="label">Socio</span><span class="value">${escapeHtml(invoice.memberName || `Socio #${invoice.memberId}`)}</span></div>
    <div><span class="label">Número de factura</span><span class="value">${escapeHtml(invoice.invoiceNumber || '—')}</span></div>
    <div><span class="label">Fecha de emisión</span><span class="value">${formatDate(invoice.invoiceDate)}</span></div>
    <div><span class="label">Estado</span><span class="value">${escapeHtml(status)}</span></div>
  </div>

  <table>
    <thead>
      <tr><th>Concepto</th><th class="number">Importe</th></tr>
    </thead>
    <tbody>
      <tr><td>Servicios de gimnasio</td><td class="number">${money(subtotal)}</td></tr>
    </tbody>
  </table>

  <div class="totals">
    <div><span>Subtotal</span><span>${money(subtotal)}</span></div>
    <div><span>IVA</span><span>${money(tax)}</span></div>
    <div class="total"><span>Total</span><span>${money(total)}</span></div>
  </div>

  ${invoice.uuid ? `<div class="uuid">UUID: ${escapeHtml(invoice.uuid)}</div>` : ''}

  <p class="note">
    Este documento es un comprobante interno generado por Centro Fit. No sustituye un CFDI
    ni un comprobante fiscal autorizado.
  </p>

  <div class="footer">
    <span>Centro Fit · Facturación</span>
    <span>${escapeHtml(invoice.memberName || '')}</span>
  </div>
</body>
</html>`;
}

export default function InvoicePrintPreview({ invoice, onClose }) {
  const frameRef = useRef(null);

  const print = () => {
    frameRef.current?.contentWindow?.focus();
    frameRef.current?.contentWindow?.print();
  };

  return (
    <Modal
      isOpen={!!invoice}
      onClose={onClose}
      title={`Factura ${invoice?.invoiceNumber || ''}`}
      size="xl"
    >
      {invoice ? (
        <div className="space-y-3">
          <iframe
            ref={frameRef}
            title="Vista previa de la factura"
            srcDoc={buildHtml(invoice)}
            className="h-[68vh] w-full rounded-lg border border-slate-200 bg-white"
          />
          <div className="flex justify-end gap-2">
            <Button variant="ghost" onClick={onClose}>Cerrar</Button>
            <Button icon={Printer} onClick={print}>Imprimir / Guardar PDF</Button>
          </div>
        </div>
      ) : (
        <p className="text-sm text-slate-500">No se pudo cargar la factura.</p>
      )}
    </Modal>
  );
}
