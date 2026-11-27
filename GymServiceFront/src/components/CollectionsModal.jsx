import { useEffect, useMemo, useState } from 'react';
import { AlertTriangle, CalendarClock, RefreshCw, Wallet } from 'lucide-react';
import api from '../services/api';
import Modal from './ui/Modal';
import Button from './ui/Button';
import Badge from './ui/Badge';
import { formatCurrency, formatDate, formatDateTime } from '../utils/format';

const stateInfo = {
  vencida: { variant: 'danger', label: 'Vencida' },
  proxima: { variant: 'warning', label: 'Próxima' },
  sin_membresia: { variant: 'neutral', label: 'Sin membresía' },
};

const filters = [
  { value: 'todas', label: 'Todas' },
  { value: 'vencida', label: 'Vencidas' },
  { value: 'proxima', label: 'Próximas' },
  { value: 'sin_membresia', label: 'Sin membresía' },
];

export default function CollectionsModal({ isOpen, onClose }) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [data, setData] = useState({ summary: {}, data: [], mode: 'shadow', days: 7 });
  const [filter, setFilter] = useState('todas');

  const load = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/members/collections', { params: { days: 7 } });
      setData(response.data || { summary: {}, data: [] });
    } catch (err) {
      setError(err.response?.data?.message || 'No se pudo cargar la cobranza.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isOpen) load();
  }, [isOpen]);

  const items = useMemo(() => {
    const list = Array.isArray(data.data) ? data.data : [];
    return filter === 'todas' ? list : list.filter((item) => item.state === filter);
  }, [data, filter]);

  const summary = data.summary || {};

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Cobranza — modo aviso" size="lg">
      <div className="space-y-4">
        <div className="flex items-start gap-2 rounded-lg border border-warning-200 bg-warning-50 px-3 py-2 text-sm text-warning-700">
          <AlertTriangle className="h-4 w-4 mt-0.5 flex-shrink-0" />
          <span>
            Este listado <strong>no bloquea accesos</strong>. Sirve para revisar vencidos y próximos
            antes de activar el bloqueo duro con el dueño.
          </span>
        </div>

        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <div className="rounded-lg border border-slate-200 bg-white px-3 py-2">
            <p className="text-xs uppercase tracking-wide text-slate-400">Vencidas</p>
            <p className="text-2xl font-bold text-danger-600">{summary.overdue || 0}</p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white px-3 py-2">
            <p className="text-xs uppercase tracking-wide text-slate-400">Próximas 7 días</p>
            <p className="text-2xl font-bold text-warning-600">{summary.upcoming || 0}</p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white px-3 py-2">
            <p className="text-xs uppercase tracking-wide text-slate-400">Sin membresía</p>
            <p className="text-2xl font-bold text-slate-700">{summary.withoutMembership || 0}</p>
          </div>
          <div className="rounded-lg border border-slate-200 bg-white px-3 py-2">
            <p className="text-xs uppercase tracking-wide text-slate-400">Crédito a favor</p>
            <p className="text-2xl font-bold text-success-600">
              {formatCurrency(summary.creditBalance || 0)}
            </p>
          </div>
        </div>

        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="flex flex-wrap gap-1">
            {filters.map((item) => (
              <Button
                key={item.value}
                size="sm"
                variant={filter === item.value ? 'primary' : 'outline'}
                onClick={() => setFilter(item.value)}
              >
                {item.label}
              </Button>
            ))}
          </div>
          <Button size="sm" variant="ghost" icon={RefreshCw} onClick={load} disabled={loading}>
            Actualizar
          </Button>
        </div>

        {error && (
          <div className="rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
            {error}
          </div>
        )}

        <div className="overflow-x-auto">
          <table className="min-w-full text-left text-sm">
            <thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-400">
              <tr>
                <th className="px-2 py-2">Socio</th>
                <th className="px-2 py-2">Contacto</th>
                <th className="px-2 py-2">Plan</th>
                <th className="px-2 py-2">Vence</th>
                <th className="px-2 py-2">Estado</th>
                <th className="px-2 py-2">Último pago</th>
                <th className="px-2 py-2 text-right">Crédito</th>
              </tr>
            </thead>
            <tbody>
              {!loading && items.length === 0 && (
                <tr>
                  <td colSpan="7" className="px-2 py-6 text-center text-slate-500">
                    No hay socios en esta lista.
                  </td>
                </tr>
              )}
              {items.map((item) => {
                const meta = stateInfo[item.state] || stateInfo.sin_membresia;
                return (
                  <tr key={item.id} className="border-b border-slate-100">
                    <td className="px-2 py-2 font-medium text-slate-800">
                      {`${item.firstName || ''} ${item.lastName || ''}`.trim() || '—'}
                    </td>
                    <td className="px-2 py-2 text-slate-500">
                      {item.phone || item.email || '—'}
                    </td>
                    <td className="px-2 py-2 text-slate-600">{item.planName || '—'}</td>
                    <td className="px-2 py-2 text-slate-600">
                      {item.endDate ? formatDate(item.endDate) : '—'}
                      {item.daysLeft != null && (
                        <p className="text-xs text-slate-400">
                          {item.daysLeft < 0
                            ? `${Math.abs(item.daysLeft)} día(s) vencida`
                            : `${item.daysLeft} día(s)`}
                        </p>
                      )}
                    </td>
                    <td className="px-2 py-2">
                      <Badge variant={meta.variant}>{meta.label}</Badge>
                    </td>
                    <td className="px-2 py-2 text-slate-500">
                      {item.lastPayment ? formatDateTime(item.lastPayment) : 'Sin pagos'}
                    </td>
                    <td className="px-2 py-2 text-right text-slate-600">
                      {item.creditBalance > 0 ? formatCurrency(item.creditBalance) : '—'}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        <div className="flex items-center justify-between text-xs text-slate-400">
          <span className="flex items-center gap-1">
            <CalendarClock className="h-3 w-3" /> Próximas: {summary.upcoming || 0}
          </span>
          <span className="flex items-center gap-1">
            <Wallet className="h-3 w-3" /> Modo sombra: solo informa
          </span>
        </div>
      </div>
    </Modal>
  );
}
