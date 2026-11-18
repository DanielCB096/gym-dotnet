import GenericPage from './GenericPage';

export default function Kiosk() {
  return <GenericPage title="Kioscos" endpoint="/kiosk/devices" columns={[
    { key: 'deviceName', label: 'Dispositivo' },
    { key: 'location', label: 'Ubicación' },
    { key: 'status', label: 'Estado' },
    { key: 'lastPing', label: 'Último ping' },
  ]} />;
}
