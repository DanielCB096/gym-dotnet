import { useCallback, useEffect, useState } from 'react';
import { AlertCircle, CheckCircle2, LockKeyhole, UnlockKeyhole } from 'lucide-react';
import api from '../services/api';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Table from '../components/ui/Table';
import Badge from '../components/ui/Badge';
import { formatCurrency, formatDateTime } from '../utils/format';

const errorMessage = (err, fallback) => err?.response?.data?.message || fallback;

export default function Cash() {
  const [current, setCurrent] = useState(null);
  const [sessions, setSessions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState(null);
  const [openingAmount, setOpeningAmount] = useState('0');
  const [closingAmount, setClosingAmount] = useState('');
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);

  const notify = (type, message) => {
    setNotice({ type, message });
    window.clearTimeout(notify.timer);
    notify.timer = window.setTimeout(() => setNotice(null), 4500);
  };

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [currentResponse, historyResponse] = await Promise.all([
        api.get('/cash-sessions/current'),
        api.get('/cash-sessions', { params: { limit: 20 } }),
      ]);
      setCurrent(currentResponse.data?.data || null);
      setSessions(historyResponse.data?.data || []);
    } catch (err) {
      setError(errorMessage(err, 'No se pudo cargar la caja.'));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  const openCash = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');
    try {
      await api.post('/cash-sessions/open', {
        openingAmount: Number(openingAmount) || 0,
        notes,
      });
      notify('success', 'Caja abierta correctamente.');
      setNotes('');
      await load();
    } catch (err) {
      setError(errorMessage(err, 'No se pudo abrir la caja.'));
    } finally {
      setSaving(false);
    }
  };

  const closeCash = async (event) => {
    event.preventDefault();
    if (!current) return;
    setSaving(true);
    setError('');
    try {
      const response = await api.post(`/cash-sessions/${current.id}/close`, {
        closingAmount: Number(closingAmount) || 0,
        notes,
      });
      const difference = Number(response.data?.difference || 0);
      notify('success', difference === 0
        ? 'Caja cerrada sin diferencias.'
        : `Caja cerrada. Diferencia: ${formatCurrency(difference)}.`);
      setClosingAmount('');
      setNotes('');
      await load();
    } catch (err) {
      setError(errorMessage(err, 'No se pudo cerrar la caja.'));
    } finally {
      setSaving(false);
    }
  };

  const totalCash = current
    ? Number(current.openingAmount) + Number(current.cashSales) + Number(current.cashMemberships)
    : 0;

  return (
    <div className="animate-fade-in space-y-5">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Caja</h1>
        <p className="mt-1 text-sm text-slate-500">Apertura, cierre y efectivo esperado del turno</p>
      </div>

      {notice && (
        <div className="flex items-start gap-2 rounded-lg border border-success-200 bg-success-50 px-3 py-2 text-sm text-success-700">
          <CheckCircle2 className="h-4 w-4 mt-0.5 flex-shrink-0" />
          <span>{notice.message}</span>
        </div>
      )}

      {error && (
        <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
          <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {!loading && !current && (
        <form onSubmit={openCash} className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-4 flex items-center gap-2">
            <UnlockKeyhole className="h-5 w-5 text-brand-600" />
            <h2 className="text-lg font-semibold text-slate-800">Abrir caja</h2>
          </div>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <Input
              label="Fondo inicial"
              type="number"
              min="0"
              step="0.01"
              value={openingAmount}
              onChange={(event) => setOpeningAmount(event.target.value)}
            />
            <Input
              label="Notas"
              value={notes}
              onChange={(event) => setNotes(event.target.value)}
              placeholder="Opcional"
              className="sm:col-span-2"
            />
          </div>
          <div className="mt-4 flex justify-end">
            <Button type="submit" icon={UnlockKeyhole} loading={saving}>Abrir caja</Button>
          </div>
        </form>
      )}

      {current && (
        <>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-5">
            {[
              { label: 'Fondo inicial', value: current.openingAmount },
              { label: 'Ventas efectivo', value: current.cashSales },
              { label: 'Membresías efectivo', value: current.cashMemberships },
              { label: 'Efectivo esperado', value: totalCash, strong: true },
              { label: 'Ventas / cobros', value: `${current.salesCount} / ${current.paymentsCount}`, text: true },
            ].map((card) => (
              <div key={card.label} className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
                <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{card.label}</p>
                <p className={`mt-2 text-xl font-bold ${card.strong ? 'text-brand-600' : 'text-slate-800'}`}>
                  {card.text ? card.value : formatCurrency(card.value)}
                </p>
              </div>
            ))}
          </div>

          <form onSubmit={closeCash} className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="mb-4 flex items-center gap-2">
              <LockKeyhole className="h-5 w-5 text-danger-500" />
              <h2 className="text-lg font-semibold text-slate-800">Cerrar caja</h2>
            </div>
            <p className="mb-4 text-sm text-slate-500">
              Abierta {formatDateTime(current.openedAt)}. Cuenta el efectivo físico e ingresa el total.
            </p>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
              <Input
                label="Efectivo contado"
                type="number"
                min="0"
                step="0.01"
                value={closingAmount}
                onChange={(event) => setClosingAmount(event.target.value)}
                required
              />
              <Input
                label="Notas de cierre"
                value={notes}
                onChange={(event) => setNotes(event.target.value)}
                placeholder="Opcional"
                className="sm:col-span-2"
              />
            </div>
            <div className="mt-4 flex justify-end">
              <Button type="submit" variant="danger" icon={LockKeyhole} loading={saving} disabled={closingAmount === ''}>
                Cerrar caja
              </Button>
            </div>
          </form>
        </>
      )}

      <div>
        <h2 className="mb-3 text-lg font-semibold text-slate-800">Historial de turnos</h2>
        <Table
          columns={[
            { header: 'Apertura', accessor: 'openedAt', render: (value) => formatDateTime(value) },
            { header: 'Cierre', accessor: 'closedAt', render: (value) => (value ? formatDateTime(value) : '—') },
            { header: 'Fondo', accessor: 'openingAmount', render: (value) => formatCurrency(value) },
            { header: 'Esperado', accessor: 'expectedCash', render: (value) => (value != null ? formatCurrency(value) : '—') },
            { header: 'Contado', accessor: 'closingAmount', render: (value) => (value != null ? formatCurrency(value) : '—') },
            {
              header: 'Diferencia',
              accessor: 'id',
              render: (_, row) => {
                if (row.closingAmount == null || row.expectedCash == null) return '—';
                const diff = Number(row.closingAmount) - Number(row.expectedCash);
                return (
                  <span className={diff === 0 ? 'text-success-600' : 'text-danger-600'}>
                    {formatCurrency(diff)}
                  </span>
                );
              },
            },
            {
              header: 'Estado',
              accessor: 'status',
              render: (value) => (
                <Badge variant={value === 'open' ? 'success' : 'neutral'} dot>
                  {value === 'open' ? 'Abierta' : 'Cerrada'}
                </Badge>
              ),
            },
          ]}
          data={sessions}
          loading={loading}
          emptyText="No hay turnos de caja"
        />
      </div>
    </div>
  );
}
