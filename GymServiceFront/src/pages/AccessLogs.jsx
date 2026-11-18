import GenericPage from './GenericPage';

export default function AccessLogs() {
  return <GenericPage title="Logs de Acceso" endpoint="/access/logs" columns={[
    { key: 'userName', label: 'Usuario' },
    { key: 'action', label: 'Acción' },
    { key: 'ipAddress', label: 'IP' },
    { key: 'timestamp', label: 'Fecha/Hora' },
  ]} />;
}
