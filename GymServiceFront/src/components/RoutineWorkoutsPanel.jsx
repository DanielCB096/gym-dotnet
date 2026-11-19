import { useCallback, useEffect, useState } from 'react';
import { AlertCircle, CheckCircle2, Plus, Trash2 } from 'lucide-react';
import api from '../services/api';
import Button from './ui/Button';
import Input from './ui/Input';
import Select from './ui/Select';
import Table from './ui/Table';
import { formatDateTime } from '../utils/format';

const errorMessage = (err, fallback) => err?.response?.data?.message || fallback;

const pad = (value) => String(value).padStart(2, '0');

const toLocalInput = (value) => {
  const date = value ? new Date(value) : new Date();
  if (Number.isNaN(date.getTime())) return '';
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
};

export default function RoutineWorkoutsPanel() {
  const [members, setMembers] = useState([]);
  const [exercises, setExercises] = useState([]);
  const [workouts, setWorkouts] = useState([]);
  const [filterMember, setFilterMember] = useState('');
  const [form, setForm] = useState({
    memberId: '',
    exerciseId: '',
    sets: 3,
    reps: 10,
    weight: '',
    durationMinutes: '',
    workoutDate: toLocalInput()
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState(null);

  const notify = (type, message) => {
    setNotice({ type, message });
    window.clearTimeout(notify.timer);
    notify.timer = window.setTimeout(() => setNotice(null), 4000);
  };

  useEffect(() => {
    Promise.allSettled([
      api.get('/members', { params: { limit: 500 } }),
      api.get('/routines/exercises'),
    ]).then(([membersResponse, exercisesResponse]) => {
      if (membersResponse.status === 'fulfilled') {
        setMembers((membersResponse.value.data?.data || []).filter((m) => m.status === 'active'));
      }
      if (exercisesResponse.status === 'fulfilled') {
        setExercises(exercisesResponse.value.data?.data || []);
      }
    });
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const params = { limit: 200 };
      if (filterMember) params.memberId = Number(filterMember);
      const response = await api.get('/routines/workouts', { params });
      setWorkouts(response.data?.data || []);
    } catch (err) {
      setWorkouts([]);
      setError(errorMessage(err, 'No se pudieron cargar los entrenamientos.'));
    } finally {
      setLoading(false);
    }
  }, [filterMember]);

  useEffect(() => { load(); }, [load]);

  const create = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');

    try {
      const payload = {
        memberId: Number(form.memberId),
        exerciseId: Number(form.exerciseId),
        sets: Number(form.sets) || 0,
        reps: Number(form.reps) || 0,
        workoutDate: form.workoutDate || undefined
      };
      if (form.weight !== '') payload.weight = Number(form.weight);
      if (form.durationMinutes !== '') payload.durationMinutes = Number(form.durationMinutes);

      await api.post('/routines/workouts', payload);
      notify('success', 'Entrenamiento registrado.');
      setForm({ ...form, exerciseId: '', weight: '', durationMinutes: '', workoutDate: toLocalInput() });
      await load();
    } catch (err) {
      setError(errorMessage(err, 'No se pudo registrar el entrenamiento.'));
    } finally {
      setSaving(false);
    }
  };

  const remove = async (row) => {
    try {
      await api.delete(`/routines/workouts/${row.id}`);
      notify('success', 'Entrenamiento eliminado.');
      await load();
    } catch (err) {
      setError(errorMessage(err, 'No se pudo eliminar el entrenamiento.'));
    }
  };

  return (
    <div className="space-y-4">
      <form onSubmit={create} className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-4">
          <Select
            label="Socio"
            value={form.memberId}
            onChange={(e) => setForm({ ...form, memberId: e.target.value })}
            options={members.map((m) => ({ value: m.id, label: `${m.firstName} ${m.lastName}` }))}
            placeholder="Selecciona"
            required
          />
          <Select
            label="Ejercicio"
            value={form.exerciseId}
            onChange={(e) => setForm({ ...form, exerciseId: e.target.value })}
            options={exercises.map((e) => ({ value: e.id, label: e.exerciseName }))}
            placeholder="Selecciona"
            required
          />
          <Input label="Series" type="number" min="1" value={form.sets} onChange={(e) => setForm({ ...form, sets: e.target.value })} />
          <Input label="Reps" type="number" min="1" value={form.reps} onChange={(e) => setForm({ ...form, reps: e.target.value })} />
          <Input label="Peso (kg)" type="number" step="0.01" value={form.weight} onChange={(e) => setForm({ ...form, weight: e.target.value })} placeholder="Opcional" />
          <Input label="Duración (min)" type="number" min="0" value={form.durationMinutes} onChange={(e) => setForm({ ...form, durationMinutes: e.target.value })} placeholder="Opcional" />
          <Input label="Fecha" type="datetime-local" value={form.workoutDate} onChange={(e) => setForm({ ...form, workoutDate: e.target.value })} />
          <div className="flex items-end">
            <Button type="submit" icon={Plus} loading={saving} disabled={!form.memberId || !form.exerciseId}>
              Registrar
            </Button>
          </div>
        </div>
      </form>

      {notice && (
        <div className="flex items-start gap-2 rounded-lg border border-success-200 bg-success-50 px-3 py-2 text-sm text-success-700">
          <CheckCircle2 className="h-4 w-4 mt-0.5 flex-shrink-0" />
          <span>{notice.message}</span>
        </div>
      )}

      {error && (
        <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
          <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
        <Select
          label="Filtrar por socio"
          value={filterMember}
          onChange={(e) => setFilterMember(e.target.value)}
          options={members.map((m) => ({ value: m.id, label: `${m.firstName} ${m.lastName}` }))}
          placeholder="Todos"
        />
        <Button variant="outline" onClick={load} className="sm:mb-0.5">Actualizar</Button>
      </div>

      <Table
        columns={[
          { header: 'Fecha', accessor: 'workoutDate', render: (value) => formatDateTime(value) },
          { header: 'Socio', accessor: 'memberName' },
          { header: 'Ejercicio', accessor: 'exerciseName' },
          { header: 'Series × Reps', accessor: 'sets', render: (value, row) => `${value} × ${row.reps}` },
          { header: 'Peso', accessor: 'weight', render: (value) => (value != null ? `${value} kg` : '—') },
          { header: 'Duración', accessor: 'durationMinutes', render: (value) => (value ? `${value} min` : '—') },
          {
            header: '',
            accessor: 'id',
            render: (_, row) => (
              <div className="flex justify-end">
                <Button variant="ghost" size="sm" icon={Trash2} onClick={() => remove(row)}>Eliminar</Button>
              </div>
            ),
          },
        ]}
        data={workouts}
        loading={loading}
        emptyText="No hay entrenamientos registrados"
      />
    </div>
  );
}
