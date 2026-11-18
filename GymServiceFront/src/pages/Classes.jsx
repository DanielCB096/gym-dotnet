import CrudPage from '../components/CrudPage';

export default function Classes() {
  return (
    <CrudPage
      title="Clases"
      subtitle="Tipos de clases grupales que ofrece el gimnasio"
      endpoint="/classes"
      createLabel="Nueva clase"
      emptyText="No hay tipos de clase registrados"
      columns={[
        { key: 'typeName', label: 'Clase' },
        { key: 'description', label: 'Descripción' },
        {
          key: 'durationMinutes',
          label: 'Duración',
          render: (row) => (row.durationMinutes ? `${row.durationMinutes} min` : '—'),
        },
        { key: 'maxParticipants', label: 'Máx. personas' },
      ]}
      fields={[
        { name: 'typeName', label: 'Nombre de la clase', required: true, placeholder: 'Ej. Spinning' },
        { name: 'description', label: 'Descripción', type: 'textarea', full: true },
        { name: 'durationMinutes', label: 'Duración (minutos)', type: 'number', min: 1, defaultValue: 60 },
        { name: 'maxParticipants', label: 'Máximo de participantes', type: 'number', min: 1, defaultValue: 30 },
      ]}
    />
  );
}
