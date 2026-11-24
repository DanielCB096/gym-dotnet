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

const difficultyLabel = (value) => {
  const labels = { beginner: 'Principiante', intermediate: 'Intermedio', advanced: 'Avanzado' };
  return labels[String(value || '').toLowerCase()] || value || '—';
};

function buildHtml({ assignment, plan, exercises }) {
  const byDay = new Map();
  [...(exercises || [])]
    .sort((a, b) => (a.dayNumber - b.dayNumber) || (a.sortOrder - b.sortOrder) || (a.id - b.id))
    .forEach((exercise) => {
      const day = Number(exercise.dayNumber) || 1;
      if (!byDay.has(day)) byDay.set(day, []);
      byDay.get(day).push(exercise);
    });

  const daySections = byDay.size === 0
    ? '<p class="empty">Esta rutina todavía no tiene ejercicios registrados.</p>'
    : [...byDay.entries()].map(([day, items]) => `
      <section class="day">
        <h2>Día ${day}</h2>
        <table>
          <thead>
            <tr>
              <th class="order">#</th>
              <th>Ejercicio</th>
              <th>Grupo muscular</th>
              <th>Equipo</th>
              <th class="number">Series</th>
              <th class="number">Reps</th>
              <th>Notas</th>
            </tr>
          </thead>
          <tbody>
            ${items.map((item, index) => `
              <tr>
                <td class="order">${index + 1}</td>
                <td><strong>${escapeHtml(item.exerciseName)}</strong></td>
                <td>${escapeHtml(item.muscleGroup || '—')}</td>
                <td>${escapeHtml(item.equipment || '—')}</td>
                <td class="number">${item.sets ?? '—'}</td>
                <td class="number">${item.reps ?? '—'}</td>
                <td class="notes">${escapeHtml(item.notes || '')}</td>
              </tr>
            `).join('')}
          </tbody>
        </table>
      </section>
    `).join('');

  return `<!doctype html>
<html lang="es">
<head>
<meta charset="utf-8" />
<title>Rutina - ${escapeHtml(assignment.memberName)}</title>
<style>
  @page { size: A4; margin: 14mm; }
  * { box-sizing: border-box; }
  body { margin: 0; color: #111827; font-family: Arial, Helvetica, sans-serif; font-size: 11.5px; }
  .header { display: flex; justify-content: space-between; align-items: flex-start; gap: 20px; border-bottom: 3px solid #465fff; padding-bottom: 12px; }
  .brand { font-size: 24px; font-weight: 800; color: #465fff; letter-spacing: -0.04em; }
  .subtitle { color: #64748b; font-size: 12px; margin-top: 2px; }
  h1 { font-size: 19px; margin: 18px 0 4px; }
  .meta { display: grid; grid-template-columns: 1fr 1fr; gap: 5px 28px; margin: 10px 0 16px; padding: 10px 12px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; }
  .meta div { display: flex; gap: 6px; }
  .label { color: #64748b; min-width: 92px; }
  .value { font-weight: 700; }
  .description { color: #475569; line-height: 1.45; margin: 0 0 14px; }
  .day { margin-top: 16px; page-break-inside: avoid; }
  h2 { font-size: 13px; margin: 0 0 6px; color: #1e293b; }
  table { width: 100%; border-collapse: collapse; }
  th, td { border: 1px solid #cbd5e1; padding: 5px 6px; text-align: left; vertical-align: top; }
  th { background: #eef2ff; color: #334155; font-size: 10.5px; text-transform: uppercase; letter-spacing: 0.03em; }
  td { font-size: 11px; }
  .order, .number { text-align: center; width: 42px; }
  .notes { color: #475569; }
  .empty { color: #64748b; font-style: italic; margin: 18px 0; }
  .footer { margin-top: 24px; padding-top: 8px; border-top: 1px solid #e2e8f0; color: #94a3b8; font-size: 10px; display: flex; justify-content: space-between; }
</style>
</head>
<body>
  <div class="header">
    <div>
      <div class="brand">Centro Fit</div>
      <div class="subtitle">Rutina de entrenamiento</div>
    </div>
    <div class="subtitle">Generada el ${formatDate(new Date())}</div>
  </div>

  <h1>${escapeHtml(plan?.planName || assignment.planName || 'Rutina')}</h1>
  <div class="meta">
    <div><span class="label">Socio</span><span class="value">${escapeHtml(assignment.memberName || '—')}</span></div>
    <div><span class="label">Asignada</span><span class="value">${formatDate(assignment.assignedAt)}</span></div>
    <div><span class="label">Dificultad</span><span class="value">${escapeHtml(difficultyLabel(plan?.difficulty))}</span></div>
    <div><span class="label">Duración</span><span class="value">${plan?.durationMinutes ? `${plan.durationMinutes} min` : '—'}</span></div>
  </div>

  ${plan?.description ? `<p class="description">${escapeHtml(plan.description)}</p>` : ''}

  ${daySections}

  <div class="footer">
    <span>Centro Fit · Rutina personalizada</span>
    <span>${escapeHtml(assignment.memberName || '')}</span>
  </div>
</body>
</html>`;
}

export default function RoutinePrintPreview({ data, onClose }) {
  const frameRef = useRef(null);

  const print = () => {
    frameRef.current?.contentWindow?.focus();
    frameRef.current?.contentWindow?.print();
  };

  return (
    <Modal
      isOpen={!!data}
      onClose={onClose}
      title={`Rutina de ${data?.assignment?.memberName || 'socio'}`}
      size="xl"
    >
      {data ? (
        <div className="space-y-3">
          <iframe
            ref={frameRef}
            title="Vista previa de la rutina"
            srcDoc={buildHtml(data)}
            className="h-[68vh] w-full rounded-lg border border-slate-200 bg-white"
          />
          <div className="flex justify-end gap-2">
            <Button variant="ghost" onClick={onClose}>Cerrar</Button>
            <Button icon={Printer} onClick={print}>Imprimir / Guardar PDF</Button>
          </div>
        </div>
      ) : (
        <p className="text-sm text-slate-500">No se pudo cargar la rutina.</p>
      )}
    </Modal>
  );
}
