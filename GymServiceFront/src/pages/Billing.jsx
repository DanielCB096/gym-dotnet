import GenericPage from './GenericPage';

export default function Billing() {
  return <GenericPage title="Facturación" endpoint="/billing/invoices" columns={[
    { key: 'invoiceNumber', label: 'No. Factura' },
    { key: 'memberName', label: 'Miembro' },
    { key: 'totalAmount', label: 'Total', render: (r) => `$${(r.totalAmount || 0).toFixed(2)}` },
    { key: 'status', label: 'Estado' },
    { key: 'issueDate', label: 'Fecha' },
  ]} />;
}
