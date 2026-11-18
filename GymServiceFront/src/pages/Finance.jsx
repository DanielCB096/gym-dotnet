import { useState } from 'react';
import { ArrowLeftRight, AlertCircle } from 'lucide-react';
import api from '../services/api';
import CrudPage from '../components/CrudPage';
import Modal from '../components/ui/Modal';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Select from '../components/ui/Select';
import { formatCurrency } from '../utils/format';

function TransactionButton({ accounts, onDone }) {
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({
    accountId: '',
    amount: '',
    transactionType: 'deposit',
    description: '',
  });
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
            id="accountId"
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
            id="amount"
            label="Monto"
            type="number"
            min="0"
            step="0.01"
            value={form.amount}
            onChange={(event) => setForm({ ...form, amount: event.target.value })}
            required
          />
          <Select
            id="transactionType"
            label="Tipo de movimiento"
            value={form.transactionType}
            onChange={(event) => setForm({ ...form, transactionType: event.target.value })}
            options={[
              { value: 'deposit', label: 'Depósito' },
              { value: 'withdrawal', label: 'Retiro' },
            ]}
          />
          <Input
            id="description"
            label="Descripción"
            value={form.description}
            onChange={(event) => setForm({ ...form, description: event.target.value })}
            placeholder="Concepto del movimiento"
          />
          {error && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={() => setOpen(false)} disabled={saving}>
              Cancelar
            </Button>
            <Button type="submit" loading={saving}>
              Registrar
            </Button>
          </div>
        </form>
      </Modal>
    </>
  );
}

export default function Finance() {
  return (
    <CrudPage
      title="Finanzas"
      subtitle="Cuentas bancarias y movimientos"
      endpoint="/finance/accounts"
      createLabel="Nueva cuenta"
      emptyText="No hay cuentas bancarias registradas"
      canEdit={false}
      columns={[
        { key: 'bankName', label: 'Banco' },
        { key: 'accountNumber', label: 'Número de cuenta' },
        {
          key: 'balance',
          label: 'Saldo',
          render: (row) => formatCurrency(row.balance, row.currency || 'MXN'),
        },
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
