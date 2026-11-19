import CrudPage from './CrudPage';

export default function ExercisesPanel() {
  return (
    <CrudPage
      hideHeader
      title="Ejercicio"
      subtitle="Catálogo de ejercicios disponibles para las rutinas"
      endpoint="/routines/exercises"
      createLabel="Nuevo ejercicio"
      emptyText="No hay ejercicios registrados"
      columns={[
        { key: 'exerciseName', label: 'Ejercicio' },
        { key: 'muscleGroup', label: 'Grupo muscular' },
        { key: 'equipment', label: 'Equipo' },
        { key: 'description', label: 'Descripción' },
      ]}
      fields={[
        { name: 'exerciseName', label: 'Nombre del ejercicio', required: true, placeholder: 'Ej. Press de banca' },
        { name: 'muscleGroup', label: 'Grupo muscular', placeholder: 'Ej. Pecho' },
        { name: 'equipment', label: 'Equipo', placeholder: 'Ej. Barra' },
        { name: 'description', label: 'Descripción', type: 'textarea', full: true },
      ]}
    />
  );
}
