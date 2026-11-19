import { useState } from 'react';
import CrudPage from '../components/CrudPage';
import ClassSessionsPanel from '../components/ClassSessionsPanel';
import PageHeader from '../components/ui/PageHeader';

export default function Classes() {
  const [tab, setTab] = useState('types');

  return (
    <div className="animate-fade-in">
      <PageHeader
        title="Clases"
        subtitle="Tipos de clase, sesiones, reservas y lista de espera"
      />

      <div className="mb-6 inline-flex rounded-lg border border-slate-200 bg-white p-1 shadow-sm">
        <button
          type="button"
          onClick={() => setTab('types')}
          className={`rounded-md px-4 py-2 text-sm font-medium transition-colors ${
            tab === 'types' ? 'bg-brand-600 text-white' : 'text-slate-600 hover:bg-slate-50'
          }`}
        >
          Tipos de clase
        </button>
        <button
          type="button"
          onClick={() => setTab('sessions')}
          className={`rounded-md px-4 py-2 text-sm font-medium transition-colors ${
            tab === 'sessions' ? 'bg-brand-600 text-white' : 'text-slate-600 hover:bg-slate-50'
          }`}
        >
          Sesiones y reservas
        </button>
      </div>

      {tab === 'types' ? (
        <CrudPage
          hideHeader
          title="Clase"
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
      ) : (
        <ClassSessionsPanel />
      )}
    </div>
  );
}
