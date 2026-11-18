import GenericPage from './GenericPage';

export default function Maintenance() {
  return <GenericPage title="Mantenimiento" endpoint="/maintenance/equipment" columns={[
    { key: 'name', label: 'Equipo' },
    { key: 'category', label: 'Categoría' },
    { key: 'status', label: 'Estado' },
    { key: 'location', label: 'Ubicación' },
  ]} />;
}
