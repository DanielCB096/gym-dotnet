import { useState } from 'react';
import { Dumbbell } from 'lucide-react';
import CrudPage from '../components/CrudPage';
import ExercisesPanel from '../components/ExercisesPanel';
import RoutineAssignmentsPanel from '../components/RoutineAssignmentsPanel';
import RoutineWorkoutsPanel from '../components/RoutineWorkoutsPanel';
import RoutineMeasurementsPanel from '../components/RoutineMeasurementsPanel';
import RoutinePlanExercises from '../components/RoutinePlanExercises';
import PageHeader from '../components/ui/PageHeader';
import Badge from '../components/ui/Badge';

const difficultyVariants = {
  beginner: { variant: 'success', label: 'Principiante' },
  intermediate: { variant: 'warning', label: 'Intermedio' },
  advanced: { variant: 'danger', label: 'Avanzado' },
};

const tabs = [
  { id: 'plans', label: 'Planes' },
  { id: 'exercises', label: 'Ejercicios' },
  { id: 'assignments', label: 'Asignaciones' },
  { id: 'workouts', label: 'Entrenamientos' },
  { id: 'measurements', label: 'Medidas' },
];

export default function Routines() {
  const [tab, setTab] = useState('plans');
  const [planExercises, setPlanExercises] = useState(null);

  return (
    <div className="animate-fade-in">
      <PageHeader
        title="Rutinas"
        subtitle="Planes, ejercicios, asignaciones, entrenamientos y medidas corporales"
      />

      <div className="mb-6 flex flex-wrap gap-1 rounded-lg border border-slate-200 bg-white p-1 shadow-sm">
        {tabs.map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => setTab(item.id)}
            className={`rounded-md px-4 py-2 text-sm font-medium transition-colors ${
              tab === item.id ? 'bg-brand-600 text-white' : 'text-slate-600 hover:bg-slate-50'
            }`}
          >
            {item.label}
          </button>
        ))}
      </div>

      {tab === 'plans' && (
        <CrudPage
          hideHeader
          title="Rutina"
          subtitle="Planes de entrenamiento disponibles"
          endpoint="/routines"
          createLabel="Nueva rutina"
          emptyText="No hay rutinas registradas"
          columns={[
            { key: 'planName', label: 'Rutina' },
            { key: 'description', label: 'Descripción' },
            {
              key: 'difficulty',
              label: 'Dificultad',
              render: (row) => {
                const info = difficultyVariants[row.difficulty] || {
                  variant: 'neutral',
                  label: row.difficulty || '—',
                };
                return <Badge variant={info.variant}>{info.label}</Badge>;
              },
            },
            {
              key: 'durationMinutes',
              label: 'Duración',
              render: (row) => (row.durationMinutes ? `${row.durationMinutes} min` : '—'),
            },
          ]}
          fields={[
            { name: 'planName', label: 'Nombre de la rutina', required: true },
            { name: 'description', label: 'Descripción', type: 'textarea', full: true },
            {
              name: 'difficulty',
              label: 'Dificultad',
              type: 'select',
              defaultValue: 'beginner',
              options: [
                { value: 'beginner', label: 'Principiante' },
                { value: 'intermediate', label: 'Intermedio' },
                { value: 'advanced', label: 'Avanzado' },
              ],
            },
            { name: 'durationMinutes', label: 'Duración (minutos)', type: 'number', min: 1, defaultValue: 45 },
          ]}
          actions={[
            {
              label: 'Ejercicios',
              icon: Dumbbell,
              onClick: (row) => setPlanExercises(row),
            },
          ]}
        />
      )}

      {tab === 'exercises' && <ExercisesPanel />}
      {tab === 'assignments' && <RoutineAssignmentsPanel />}
      {tab === 'workouts' && <RoutineWorkoutsPanel />}
      {tab === 'measurements' && <RoutineMeasurementsPanel />}

      <RoutinePlanExercises plan={planExercises} onClose={() => setPlanExercises(null)} />
    </div>
  );
}
