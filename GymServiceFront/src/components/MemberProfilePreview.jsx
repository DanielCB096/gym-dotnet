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

const membershipLabels = {
  active: 'Activa',
  frozen: 'Congelada',
  cancelled: 'Cancelada',
  expired: 'Vencida',
  none: 'Sin membresía',
};

function row(label, value) {
  return `<div class="field"><span class="label">${escapeHtml(label)}</span><span class="value">${escapeHtml(value || '—')}</span></div>`;
}

function buildHtml(member) {
  const initials = `${member.firstName || ''} ${member.lastName || ''}`
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part.charAt(0).toUpperCase())
    .join('');

  const photoUrl = member.photoUrl
    ? (member.photoUrl.startsWith('http') ? member.photoUrl : `${window.location.origin}${member.photoUrl}`)
    : '';

  const membership = member.membershipType
    ? `
      <div class="membership">
        <div>
          <span class="label">Plan</span>
          <strong>${escapeHtml(member.membershipType)}</strong>
        </div>
        <div>
          <span class="label">Estado</span>
          <strong>${escapeHtml(membershipLabels[member.membershipStatus] || member.membershipStatus || '—')}</strong>
        </div>
        <div>
          <span class="label">Inicio</span>
          <strong>${formatDate(member.membershipStart)}</strong>
        </div>
        <div>
          <span class="label">Vence</span>
          <strong>${formatDate(member.membershipEnd)}</strong>
        </div>
        <div>
          <span class="label">Crédito</span>
          <strong>${money(member.creditBalance)}</strong>
        </div>
      </div>
    `
    : '<p class="empty">El socio no tiene una membresía asignada.</p>';

  return `<!doctype html>
<html lang="es">
<head>
<meta charset="utf-8" />
<title>Ficha - ${escapeHtml(member.firstName)} ${escapeHtml(member.lastName)}</title>
<style>
  @page { size: A4; margin: 14mm; }
  * { box-sizing: border-box; }
  body { margin: 0; color: #111827; font-family: Arial, Helvetica, sans-serif; font-size: 11.5px; }
  .header { display: flex; justify-content: space-between; align-items: flex-start; gap: 20px; border-bottom: 3px solid #465fff; padding-bottom: 12px; }
  .brand { font-size: 24px; font-weight: 800; color: #465fff; letter-spacing: -0.04em; }
  .subtitle { color: #64748b; font-size: 12px; margin-top: 2px; }
  .identity { display: flex; gap: 14px; align-items: center; margin: 18px 0 14px; }
  .avatar { width: 48px; height: 48px; border-radius: 50%; background: #eef2ff; color: #465fff; display: flex; align-items: center; justify-content: center; font-size: 18px; font-weight: 800; }
  .photo { width: 56px; height: 56px; border-radius: 50%; object-fit: cover; border: 1px solid #e2e8f0; }
  h1 { font-size: 21px; margin: 0 0 3px; }
  h2 { font-size: 13px; margin: 18px 0 7px; color: #1e293b; border-bottom: 1px solid #e2e8f0; padding-bottom: 4px; }
  .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 7px 28px; }
  .field { display: flex; gap: 6px; min-height: 16px; }
  .label { color: #64748b; min-width: 112px; }
  .value { font-weight: 600; }
  .wide { grid-column: 1 / -1; }
  .membership { display: grid; grid-template-columns: repeat(5, 1fr); gap: 8px; padding: 10px 12px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; }
  .membership > div { display: flex; flex-direction: column; gap: 3px; }
  .empty { color: #64748b; font-style: italic; }
  .notes { margin: 0; padding: 9px 11px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; color: #475569; line-height: 1.45; white-space: pre-wrap; }
  .footer { margin-top: 26px; padding-top: 8px; border-top: 1px solid #e2e8f0; color: #94a3b8; font-size: 10px; display: flex; justify-content: space-between; }
</style>
</head>
<body>
  <div class="header">
    <div>
      <div class="brand">Centro Fit</div>
      <div class="subtitle">Ficha del socio</div>
    </div>
    <div class="subtitle">Generada el ${formatDate(new Date())}</div>
  </div>

  <div class="identity">
    ${photoUrl
      ? `<img class="photo" src="${escapeHtml(photoUrl)}" alt="" />`
      : `<div class="avatar">${escapeHtml(initials || 'S')}</div>`}
    <div>
      <h1>${escapeHtml(`${member.firstName || ''} ${member.lastName || ''}`.trim())}</h1>
      <div class="subtitle">${escapeHtml(member.status === 'active' ? 'Socio activo' : 'Socio inactivo')}</div>
    </div>
  </div>

  <h2>Datos personales</h2>
  <div class="grid">
    ${row('Email', member.email)}
    ${row('Teléfono', member.phone)}
    ${row('CURP', member.curp)}
    ${row('RFC', member.rfc)}
    ${row('Dirección', member.address)}
    ${row('Contacto de emergencia', member.emergencyContact)}
    ${row('Teléfono de emergencia', member.emergencyPhone)}
  </div>

  <h2>Membresía</h2>
  ${membership}

  <h2>Notas</h2>
  <p class="notes">${escapeHtml(member.notes || 'Sin notas registradas.')}</p>

  <div class="footer">
    <span>Centro Fit · Ficha del socio</span>
    <span>${escapeHtml(member.email || '')}</span>
  </div>
</body>
</html>`;
}

export default function MemberProfilePreview({ member, onClose }) {
  const frameRef = useRef(null);

  const print = () => {
    frameRef.current?.contentWindow?.focus();
    frameRef.current?.contentWindow?.print();
  };

  return (
    <Modal
      isOpen={!!member}
      onClose={onClose}
      title={`Ficha de ${member?.firstName || 'socio'} ${member?.lastName || ''}`}
      size="xl"
    >
      {member ? (
        <div className="space-y-3">
          <iframe
            ref={frameRef}
            title="Vista previa de la ficha del socio"
            srcDoc={buildHtml(member)}
            className="h-[68vh] w-full rounded-lg border border-slate-200 bg-white"
          />
          <div className="flex justify-end gap-2">
            <Button variant="ghost" onClick={onClose}>Cerrar</Button>
            <Button icon={Printer} onClick={print}>Imprimir / Guardar PDF</Button>
          </div>
        </div>
      ) : (
        <p className="text-sm text-slate-500">No se pudo cargar la ficha.</p>
      )}
    </Modal>
  );
}
