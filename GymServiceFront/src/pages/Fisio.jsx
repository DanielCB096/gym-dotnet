import GenericPage from './GenericPage';

export default function Fisio() {
  return <GenericPage title="Fisioterapia" endpoint="/fisio/packages" columns={[
    { key: 'packageName', label: 'Paquete' },
    { key: 'sessions', label: 'Sesiones' },
    { key: 'price', label: 'Precio', render: (r) => `$${(r.price || 0).toFixed(2)}` },
    { key: 'status', label: 'Estado' },
  ]} />;
}
