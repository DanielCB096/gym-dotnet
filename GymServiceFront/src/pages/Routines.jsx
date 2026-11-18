import GenericPage from './GenericPage';

export default function Routines() {
  return <GenericPage title="Rutinas" endpoint="/routines" columns={[
    { key: 'name', label: 'Rutina' },
    { key: 'type', label: 'Tipo' },
    { key: 'duration', label: 'Duración' },
    { key: 'difficulty', label: 'Dificultad' },
  ]} />;
}
