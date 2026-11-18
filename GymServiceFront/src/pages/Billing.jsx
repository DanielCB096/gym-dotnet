import CrudPage from '../components/CrudPage';
import Badge from '../components/ui/Badge';
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
  return (
    <CrudPage
      title="Facturación"
      subtitle="Facturas emitidas a los socios"
      endpoint="/billing/invoices"
      createLabel="Nueva factura"
      emptyText="No hay facturas registradas"
      columns={[
        { key: 'invoiceNumber', label: 'No. factura' },
        { key: 'memberId', label: 'Miembro', render: (row) => `#${row.memberId}` },
        { key: 'amount', label: 'Total', render: (row) => formatCurrency(row.amount) },
        {
          key: 'status',
          label: 'Estado',
          render: (row) => {
            const info = statusInfo[row.status] || { variant: 'neutral', label: row.status || '—' };
            return <Badge variant={info.variant}>{info.label}</Badge>;
          },
        },
        { key: 'invoiceDate', label: 'Fecha', render: (row) => formatDate(row.invoiceDate) },
      ]}
      fields={[
        { name: 'invoiceNumber', label: 'Número de factura', required: true },
        { name: 'memberId', label: 'ID del miembro', type: 'number', required: true, min: 1 },
        { name: 'amount', label: 'Monto', type: 'number', min: 0, step: '0.01', defaultValue: 0 },
        { name: 'status', label: 'Estado', type: 'select', defaultValue: 'pending', options: statusOptions },
      ]}
      editFields={[
        { name: 'status', label: 'Estado', type: 'select', defaultValue: 'pending', options: statusOptions },
      ]}
      updatePath={(invoice) => `/billing/invoices/${invoice.id}/status`}
      updatePayload={(form) => ({ status: form.status })}
    />
  );
}
