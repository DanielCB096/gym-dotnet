import GenericPage from './GenericPage';

export default function Reports() {
  return <GenericPage title="Reportes Programados" endpoint="/reports/scheduled" columns={[
    { key: 'reportName', label: 'Reporte' },
    { key: 'schedule', label: 'Programación' },
    { key: 'format', label: 'Formato' },
    { key: 'isActive', label: 'Activo', render: (r) => r.isActive ? 'Sí' : 'No' },
  ]} />;
}
