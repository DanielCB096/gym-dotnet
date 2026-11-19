import { useCallback, useEffect, useState } from 'react';
import {
  AlertCircle,
  ArrowLeftRight,
  CheckCircle2,
  Flag,
  Landmark,
  RotateCcw,
  Upload,
} from 'lucide-react';
import api from '../services/api';
import CrudPage from '../components/CrudPage';
import PageHeader from '../components/ui/PageHeader';
import Badge from '../components/ui/Badge';
import Modal from '../components/ui/Modal';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Select from '../components/ui/Select';
import Table from '../components/ui/Table';
import ConfirmDialog from '../components/ui/ConfirmDialog';
import { formatCurrency, formatDateTime } from '../utils/format';

function TransactionButton({ accounts, onDone }) {
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({ accountId: '', amount: '', transactionType: 'deposit', description: '' });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const openModal = () => {
    setForm({ accountId: '', amount: '', transactionType: 'deposit', description: '' });
    setError('');
    setOpen(true);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!form.accountId) {
      setError('Selecciona una cuenta.');
      return;
    }
    setSaving(true);
    setError('');
    try {
      const response = await api.post('/finance/transactions', {
        accountId: Number(form.accountId),
        amount: Number(form.amount) || 0,
        transactionType: form.transactionType,
        description: form.description,
      });
      onDone(response.data?.message || 'Movimiento registrado correctamente.');
      setOpen(false);
    } catch (err) {
      setError(err.response?.data?.message || 'No se pudo registrar el movimiento.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <Button variant="outline" icon={ArrowLeftRight} onClick={openModal} disabled={accounts.length === 0}>
        Registrar movimiento
      </Button>
      <Modal isOpen={open} onClose={() => !saving && setOpen(false)} title="Registrar movimiento" size="md">
        <form onSubmit={handleSubmit} className="space-y-4">
          <Select
            label="Cuenta"
            value={form.accountId}
            onChange={(event) => setForm({ ...form, accountId: event.target.value })}
            placeholder="Selecciona una cuenta"
            options={accounts.map((account) => ({
              value: account.id,
              label: `${account.bankName} · ${account.accountNumber}`,
            }))}
          />
          <Input
            label="Monto"
            type="number"
            min="0"
            step="0.01"
            value={form.amount}
            onChange={(event) => setForm({ ...form, amount: event.target.value })}
            required
          />
          <Select
            label="Tipo de movimiento"
            value={form.transactionType}
            onChange={(event) => setForm({ ...form, transactionType: event.target.value })}
            options={[
              { value: 'deposit', label: 'Depósito' },
              { value: 'withdrawal', label: 'Retiro' },
            ]}
          />
          <Input
            label="Descripción"
            value={form.description}
            onChange={(event) => setForm({ ...form, description: event.target.value })}
            placeholder="Concepto del movimiento"
          />
          {error && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={() => setOpen(false)} disabled={saving}>Cancelar</Button>
            <Button type="submit" loading={saving}>Registrar</Button>
          </div>
        </form>
      </Modal>
    </>
  );
}

const errorMessage = (err, fallback) => err?.response?.data?.message || fallback;

function AccountsPanel() {
  return (
    <CrudPage
      hideHeader
      title="Cuenta"
      subtitle="Cuentas bancarias"
      endpoint="/finance/accounts"
      createLabel="Nueva cuenta"
      emptyText="No hay cuentas bancarias registradas"
      canEdit={false}
      columns={[
        { key: 'bankName', label: 'Banco' },
        { key: 'accountNumber', label: 'Número de cuenta' },
        { key: 'balance', label: 'Saldo', render: (row) => formatCurrency(row.balance, row.currency || 'MXN') },
        { key: 'currency', label: 'Moneda' },
      ]}
      fields={[
        { name: 'bankName', label: 'Banco', required: true },
        { name: 'accountNumber', label: 'Número de cuenta', required: true },
        { name: 'balance', label: 'Saldo inicial', type: 'number', step: '0.01', defaultValue: 0 },
        {
          name: 'currency',
          label: 'Moneda',
          type: 'select',
          defaultValue: 'MXN',
          options: [
            { value: 'MXN', label: 'Peso mexicano (MXN)' },
            { value: 'USD', label: 'Dólar (USD)' },
            { value: 'EUR', label: 'Euro (EUR)' },
          ],
        },
      ]}
      toolbar={({ items, reload, notify }) => (
        <TransactionButton
          accounts={items}
          onDone={(message) => {
            notify('success', message);
            reload();
          }}
        />
      )}
    />
  );
}

const normalize = (value) => String(value || '')
  .trim()
  .toLowerCase()
  .normalize('NFD')
  .replace(/[\u0300-\u036f]/g, '');

function parseCsv(text) {
  const lines = text.split(/\r?\n/).filter((line) => line.trim() !== '');
  if (lines.length < 2) return [];

  const split = (line) => line.split(/[;,]/).map((cell) => cell.trim().replace(/^"|"$/g, ''));
  const headers = split(lines[0]);
  const find = (...names) => headers.findIndex((header) => names.includes(normalize(header)));

  const dateIndex = find('fecha', 'date', 'transactiondate');
  const typeIndex = find('tipo', 'type', 'transactiontype');
  const amountIndex = find('monto', 'amount', 'importe');
  const descriptionIndex = find('descripcion', 'description', 'concepto');
  const referenceIndex = find('referencia', 'reference', 'folio');

  if (amountIndex < 0) return [];

  return lines.slice(1).map((line) => {
    const cells = split(line);
    const rawType = normalize(cells[typeIndex]);
    const type = ['retiro', 'withdrawal', 'cargo', 'salida'].includes(rawType) ? 'withdrawal' : 'deposit';
    return {
      transactionDate: cells[dateIndex] || new Date().toISOString(),
      transactionType: type,
      amount: Number(String(cells[amountIndex] || '0').replace(/[^0-9.-]/g, '')) || 0,
      description: cells[descriptionIndex] || 'Movimiento importado',
      reference: cells[referenceIndex] || '',
    };
  }).filter((movement) => movement.amount > 0);
}

function MovementsPanel() {
  const [accounts, setAccounts] = useState([]);
  const [movements, setMovements] = useState([]);
  const [summary, setSummary] = useState(null);
  const [filters, setFilters] = useState({ accountId: '', reconciled: '', flagged: '' });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState(null);
  const [confirm, setConfirm] = useState(null);
  const [confirmLoading, setConfirmLoading] = useState(false);
  const [importOpen, setImportOpen] = useState(false);
  const [importAccount, setImportAccount] = useState('');
  const [importError, setImportError] = useState('');
  const [importing, setImporting] = useState(false);

  const notify = (type, message) => {
    setNotice({ type, message });
    window.clearTimeout(notify.timer);
    notify.timer = window.setTimeout(() => setNotice(null), 4500);
  };

  const loadAccounts = useCallback(async () => {
    try {
      const response = await api.get('/finance/accounts');
      setAccounts(response.data?.data || []);
    } catch {
      setAccounts([]);
    }
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const params = { limit: 200 };
      if (filters.accountId) params.accountId = Number(filters.accountId);
      if (filters.reconciled) params.reconciled = filters.reconciled === 'true';
      if (filters.flagged) params.flagged = filters.flagged === 'true';

      const [movementsResponse, summaryResponse] = await Promise.all([
        api.get('/finance/transactions', { params }),
        api.get('/finance/reconciliation', {
          params: filters.accountId ? { accountId: Number(filters.accountId) } : undefined,
        }),
      ]);

      setMovements(movementsResponse.data?.data || []);
      setSummary(summaryResponse.data?.data || null);
    } catch (err) {
      setMovements([]);
      setSummary(null);
      setError(errorMessage(err, 'No se pudieron cargar los movimientos.'));
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => { loadAccounts(); }, [loadAccounts]);
  useEffect(() => { load(); }, [load]);

  const runAction = async (action, message) => {
    try {
      const response = await action();
      notify('success', response.data?.message || message);
      await load();
    } catch (err) {
      notify('error', errorMessage(err, 'No se pudo completar la acción.'));
    }
  };

  const reconcile = (movement) => {
    setConfirm({
      title: 'Conciliar movimiento',
      message: 'El movimiento se marcará como conciliado sin vincular una venta o pago. ¿Continuar?',
      variant: 'primary',
      action: () => api.post(`/finance/transactions/${movement.id}/reconcile`, { withType: 'manual', notes: '' }),
    });
  };

  const toggleFlag = (movement) => {
    runAction(
      () => api.post(`/finance/transactions/${movement.id}/flag`, {
        flagged: !movement.flagged,
        notes: movement.flagged ? '' : 'Marcado para revisión',
      }),
      movement.flagged ? 'Marca retirada' : 'Movimiento marcado',
    );
  };

  const runConfirm = async () => {
    if (!confirm) return;
    setConfirmLoading(true);
    try {
      await confirm.action();
      setConfirm(null);
      notify('success', 'Movimiento conciliado.');
      await load();
    } catch (err) {
      notify('error', errorMessage(err, 'No se pudo conciliar el movimiento.'));
      setConfirm(null);
    } finally {
      setConfirmLoading(false);
    }
  };

  const handleImport = async (event) => {
    event.preventDefault();
    const file = event.target.querySelector('input[type="file"]')?.files?.[0];
    if (!importAccount || !file) {
      setImportError('Selecciona una cuenta y un archivo CSV.');
      return;
    }

    setImporting(true);
    setImportError('');
    try {
      const text = await file.text();
      const movements = parseCsv(text);
      if (movements.length === 0) {
        setImportError('El CSV no tiene movimientos válidos. Usa columnas fecha, tipo, monto, descripción, referencia.');
        return;
      }
      const response = await api.post(`/finance/accounts/${importAccount}/movements/import`, { movements });
      notify('success', response.data?.message || 'Movimientos importados.');
      setImportOpen(false);
      await load();
    } catch (err) {
      setImportError(errorMessage(err, 'No se pudieron importar los movimientos.'));
    } finally {
      setImporting(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm lg:flex-row lg:items-end">
        <Select
          label="Cuenta"
          value={filters.accountId}
          onChange={(event) => setFilters({ ...filters, accountId: event.target.value })}
          options={accounts.map((account) => ({ value: account.id, label: `${account.bankName} · ${account.accountNumber}` }))}
          placeholder="Todas"
        />
        <Select
          label="Conciliación"
          value={filters.reconciled}
          onChange={(event) => setFilters({ ...filters, reconciled: event.target.value })}
          options={[
            { value: 'false', label: 'Pendientes' },
            { value: 'true', label: 'Conciliados' },
          ]}
          placeholder="Todos"
        />
        <Select
          label="Marca"
          value={filters.flagged}
          onChange={(event) => setFilters({ ...filters, flagged: event.target.value })}
          options={[{ value: 'true', label: 'Solo marcados' }]}
          placeholder="Todos"
        />
        <Button variant="outline" icon={Upload} onClick={() => { setImportError(''); setImportAccount(filters.accountId || ''); setImportOpen(true); }}>
          Importar CSV
        </Button>
      </div>

      {summary && (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {[
            { label: 'Pendientes', value: summary.pendingCount, amount: summary.pendingAmount },
            { label: 'Conciliados', value: summary.reconciledCount, amount: summary.reconciledAmount },
            { label: 'Marcados', value: summary.flaggedCount },
            { label: 'Total movimientos', value: summary.total },
          ].map((card) => (
            <div key={card.label} className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{card.label}</p>
              <p className="mt-1 text-xl font-bold text-slate-800">{card.value}</p>
              {card.amount != null && <p className="text-xs text-slate-400">{formatCurrency(card.amount)}</p>}
            </div>
          ))}
        </div>
      )}

      {notice && (
        <div className={`flex items-start gap-2 rounded-lg border px-3 py-2 text-sm ${
          notice.type === 'error'
            ? 'border-danger-200 bg-danger-50 text-danger-700'
            : 'border-success-200 bg-success-50 text-success-700'
        }`}>
          {notice.type === 'error'
            ? <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
            : <CheckCircle2 className="h-4 w-4 mt-0.5 flex-shrink-0" />}
          <span>{notice.message}</span>
        </div>
      )}

      {error && (
        <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
          <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <Table
        columns={[
          { header: 'Fecha', accessor: 'transactionDate', render: (value) => formatDateTime(value) },
          { header: 'Cuenta', accessor: 'bankName' },
          {
            header: 'Tipo',
            accessor: 'transactionType',
            render: (value) => (
              <Badge variant={value === 'deposit' ? 'success' : 'warning'}>
                {value === 'deposit' ? 'Depósito' : 'Retiro'}
              </Badge>
            ),
          },
          {
            header: 'Importe',
            accessor: 'amount',
            render: (value, row) => (
              <span className={row.transactionType === 'deposit' ? 'font-medium text-success-600' : 'font-medium text-danger-600'}>
                {row.transactionType === 'deposit' ? '+' : '-'}{formatCurrency(value)}
              </span>
            ),
          },
          { header: 'Descripción', accessor: 'description', render: (value) => value || '—' },
          { header: 'Referencia', accessor: 'reference', render: (value) => value || '—' },
          {
            header: 'Origen',
            accessor: 'source',
            render: (value) => <Badge variant={value === 'import' ? 'info' : 'neutral'}>{value === 'import' ? 'Importado' : 'Manual'}</Badge>,
          },
          {
            header: 'Estado',
            accessor: 'reconciled',
            render: (value, row) => (
              <div className="flex flex-wrap gap-1">
                <Badge variant={value ? 'success' : 'warning'} dot>{value ? 'Conciliado' : 'Pendiente'}</Badge>
                {row.flagged && <Badge variant="danger">Marcado</Badge>}
              </div>
            ),
          },
          {
            header: '',
            accessor: 'id',
            render: (_, row) => (
              <div className="flex items-center justify-end gap-1">
                {row.reconciled ? (
                  <Button
                    variant="ghost"
                    size="sm"
                    icon={RotateCcw}
                    onClick={() => runAction(
                      () => api.post(`/finance/transactions/${row.id}/unreconcile`),
                      'Movimiento desconciliado',
                    )}
                  >
                    Desconciliar
                  </Button>
                ) : (
                  <Button variant="ghost" size="sm" icon={CheckCircle2} onClick={() => reconcile(row)}>
                    Conciliar
                  </Button>
                )}
                <Button variant="ghost" size="sm" icon={Flag} onClick={() => toggleFlag(row)}>
                  {row.flagged ? 'Quitar marca' : 'Marcar'}
                </Button>
              </div>
            ),
          },
        ]}
        data={movements}
        loading={loading}
        emptyText="No hay movimientos bancarios"
        emptyIcon={Landmark}
      />

      <Modal isOpen={importOpen} onClose={() => !importing && setImportOpen(false)} title="Importar estado de cuenta" size="md">
        <form onSubmit={handleImport} className="space-y-4">
          <Select
            label="Cuenta"
            value={importAccount}
            onChange={(event) => setImportAccount(event.target.value)}
            options={accounts.map((account) => ({ value: account.id, label: `${account.bankName} · ${account.accountNumber}` }))}
            placeholder="Selecciona una cuenta"
            required
          />
          <div>
            <label htmlFor="csvFile" className="block text-sm font-medium text-slate-700 mb-1.5">
              Archivo CSV
            </label>
            <input
              id="csvFile"
              type="file"
              accept=".csv,text/csv"
              className="block w-full rounded-lg border border-slate-300 bg-white px-3.5 py-2.5 text-sm"
            />
            <p className="mt-1 text-xs text-slate-400">
              Columnas: fecha, tipo (depósito/retiro), monto, descripción, referencia.
            </p>
          </div>

          {importError && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{importError}</span>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={() => setImportOpen(false)} disabled={importing}>Cancelar</Button>
            <Button type="submit" icon={Upload} loading={importing} disabled={!importAccount}>Importar</Button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!confirm}
        onClose={() => setConfirm(null)}
        onConfirm={runConfirm}
        loading={confirmLoading}
        title={confirm?.title || 'Confirmar'}
        message={confirm?.message || '¿Deseas continuar?'}
        confirmLabel="Continuar"
        variant={confirm?.variant === 'danger' ? 'danger' : 'primary'}
      />
    </div>
  );
}

export default function Finance() {
  const [tab, setTab] = useState('accounts');

  return (
    <div className="animate-fade-in">
      <PageHeader
        title="Finanzas"
        subtitle="Cuentas bancarias, movimientos y conciliación"
      />

      <div className="mb-6 inline-flex rounded-lg border border-slate-200 bg-white p-1 shadow-sm">
        <button
          type="button"
          onClick={() => setTab('accounts')}
          className={`rounded-md px-4 py-2 text-sm font-medium transition-colors ${
            tab === 'accounts' ? 'bg-brand-600 text-white' : 'text-slate-600 hover:bg-slate-50'
          }`}
        >
          Cuentas
        </button>
        <button
          type="button"
          onClick={() => setTab('movements')}
          className={`rounded-md px-4 py-2 text-sm font-medium transition-colors ${
            tab === 'movements' ? 'bg-brand-600 text-white' : 'text-slate-600 hover:bg-slate-50'
          }`}
        >
          Movimientos y conciliación
        </button>
      </div>

      {tab === 'accounts' ? <AccountsPanel /> : <MovementsPanel />}
    </div>
  );
}
