import GenericPage from './GenericPage';

export default function Classes() {
  return <GenericPage title="Clases" endpoint="/classes" columns={[
    { key: 'name', label: 'Clase' },
    { key: 'instructor', label: 'Instructor' },
    { key: 'schedule', label: 'Horario' },
    { key: 'maxParticipants', label: 'Máx. personas' },
  ]} />;
}
