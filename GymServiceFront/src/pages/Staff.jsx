import GenericPage from './GenericPage';

export default function Staff() {
  return <GenericPage title="Personal" endpoint="/staff" columns={[
    { key: 'name', label: 'Nombre' },
    { key: 'role', label: 'Rol' },
    { key: 'email', label: 'Email' },
    { key: 'phone', label: 'Teléfono' },
    { key: 'status', label: 'Estado' },
  ]} />;
}
