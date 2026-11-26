import { useEffect, useState } from 'react';
import { Printer } from 'lucide-react';
import api from '../services/api';
import CrudPage from '../components/CrudPage';
import Badge from '../components/ui/Badge';
import Button from '../components/ui/Button';
import Select from '../components/ui/Select';
import InvoicePrintPreview from '../components/InvoicePrintPreview';
import { formatCurrency, formatDate } from '../utils/format';

const statusInfo = {
  pending: { variant: 'warning', label: 'Pendiente' },
  paid: { variant: 'success', label: 'Pagada' },
  cancelled: { variant: 'danger', label: 'Cancelada' },
};

const statusOptions = [
  { value: 'pending', label: 'Pendiente' },
  { value: 'paid', label: 'Pagada' },
  { value: 'cancelled', label: 'Cancelada' },
];

export default function Billing() {
  const [members, setMembers] = useState([]);
  const [statusFilter, setStatusFilter] = useState('');
  const [printInvoice, setPrintInvoice] = useState(null);

  useEffect(() => {
    let active = true;

    api.get('/members', { params: { limit: 500 } })
      .then((response) => {
        if (active) setMembers(response.data?.data || []);
      })
      .catch(() => {
        if (active) setMembers([]);
      });

    return () => { active = false; };
  }, []);

  const memberOptions = members.map((member) => ({
    value: member.id,
    label: `${member.firstName} ${member.lastName}${member.email ? ` · ${member.email}` : ''}`,
  }));

  const createFields = [
    {
      name: 'invoiceNumber',
      label: 'No. de factura',
      placeholder: 'Automático si se deja vacío',
    },
    {
      name: 'memberId',
      label: 'Socio',
      type: 'select',
      required: true,
      options: memberOptions,
      placeholder: 'Selecciona un socio',
    },
    { name: 'amount', label: 'Importe total', type: 'number', min: 0.01, step: '0.01', defaultValue: 0 },
    { name: 'taxAmount', label: 'IVA incluido', type: 'number', min: 0, step: '0.01', defaultValue: 0 },
    {
      name: 'status',
      label: 'Estado',
      type: 'select',
      defaultValue: 'pending',
      options: statusOptions,
    },
  ];

  return (
    <>
    <CrudPage
      title="Facturación"
      subtitle="Facturas emitidas a los socios"
      endpoint="/billing/invoices"
      extraParams={statusFilter ? { status: statusFilter } : undefined}
      createLabel="Nueva factura"
      emptyText="No hay facturas registradas"
      searchParam="query"
      searchPlaceholder="Buscar factura"
      toolbar={({ reload }) => (
        <div className="flex items-end gap-2">
          <Select
            label="Estado"
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value)}
            options={statusOptions}
            placeholder="Todos"
          />
          <Button variant="outline" onClick={reload}>Actualizar</Button>
        </div>
      )}
      columns={[
        { key: 'invoiceNumber', label: 'No. de factura' },
        {
          key: 'memberName',
          label: 'Socio',
          render: (row) => (
            <div>
              <p className="font-medium text-slate-800">{row.memberName || `Socio #${row.memberId}`}</p>
              <p className="text-xs text-slate-400">#{row.memberId}</p>
            </div>
          ),
        },
        { key: 'amount', label: 'Importe', render: (row) => formatCurrency(row.amount) },
        {
          key: 'taxAmount',
          label: 'IVA',
          render: (row) => (row.taxAmount != null ? formatCurrency(row.taxAmount) : '—'),
        },
        {
          key: 'status',
          label: 'Estado',
          render: (row) => {
            const info = statusInfo[row.status] || { variant: 'neutral', label: row.status || '—' };
            return <Badge variant={info.variant} dot>{info.label}</Badge>;
          },
        },
        { key: 'invoiceDate', label: 'Fecha', render: (row) => formatDate(row.invoiceDate) },
      ]}
      actions={[
        {
          label: 'Imprimir',
          icon: Printer,
          onClick: (row) => setPrintInvoice(row),
        },
      ]}
      fields={createFields}
      createFields={createFields}
      editFields={[
        { name: 'status', label: 'Estado', type: 'select', options: statusOptions },
      ]}
      updatePath={(invoice) => `/billing/invoices/${invoice.id}/status`}
      updatePayload={(form) => ({ status: form.status })}
    />

    <InvoicePrintPreview invoice={printInvoice} onClose={() => setPrintInvoice(null)} />
    </>
  );
}
