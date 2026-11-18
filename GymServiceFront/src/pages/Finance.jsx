import GenericPage from './GenericPage';

export default function Finance() {
  return <GenericPage title="Finanzas" endpoint="/finance/accounts" columns={[
    { key: 'accountName', label: 'Cuenta' },
    { key: 'accountType', label: 'Tipo' },
    { key: 'balance', label: 'Saldo', render: (r) => `$${(r.balance || 0).toFixed(2)}` },
    { key: 'status', label: 'Estado' },
  ]} />;
}
