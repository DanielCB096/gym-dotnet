import CrudPage from '../components/CrudPage';
import Badge from '../components/ui/Badge';

const difficultyVariants = {
  beginner: { variant: 'success', label: 'Principiante' },
  intermediate: { variant: 'warning', label: 'Intermedio' },
  advanced: { variant: 'danger', label: 'Avanzado' },
};

export default function Routines() {
  return (
    <CrudPage
      title="Rutinas"
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
    />
  );
}
