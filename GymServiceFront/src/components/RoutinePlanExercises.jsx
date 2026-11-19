import { useCallback, useEffect, useState } from 'react';
import { AlertCircle, Plus, Trash2 } from 'lucide-react';
import api from '../services/api';
import Modal from './ui/Modal';
import Button from './ui/Button';
import Input from './ui/Input';
import Select from './ui/Select';
import Table from './ui/Table';
import ConfirmDialog from './ui/ConfirmDialog';

const errorMessage = (err, fallback) => err?.response?.data?.message || fallback;

export default function RoutinePlanExercises({ plan, onClose }) {
  const [catalog, setCatalog] = useState([]);
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState({ exerciseId: '', dayNumber: 1, sortOrder: 1, sets: 3, reps: 12, notes: '' });
  const [confirm, setConfirm] = useState(null);
  const [confirmLoading, setConfirmLoading] = useState(false);

  useEffect(() => {
    let active = true;
    api.get('/routines/exercises')
      .then((response) => { if (active) setCatalog(response.data?.data || []); })
      .catch(() => { if (active) setCatalog([]); });
    return () => { active = false; };
  }, []);

  const load = useCallback(async () => {
    if (!plan) return;
    setLoading(true);
    setError('');
    try {
      const response = await api.get(`/routines/${plan.id}/exercises`);
      setItems(response.data?.data || []);
    } catch (err) {
      setItems([]);
      setError(errorMessage(err, 'No se pudieron cargar los ejercicios del plan.'));
    } finally {
      setLoading(false);
    }
  }, [plan]);

  useEffect(() => { load(); }, [load]);

  const add = async (event) => {
    event.preventDefault();
    if (!form.exerciseId) return;

    setSaving(true);
    setError('');

    try {
      await api.post(`/routines/${plan.id}/exercises`, {
        exerciseId: Number(form.exerciseId),
        dayNumber: Number(form.dayNumber) || 1,
        sortOrder: Number(form.sortOrder) || 1,
        sets: form.sets === '' ? undefined : Number(form.sets),
        reps: form.reps === '' ? undefined : Number(form.reps),
        notes: form.notes || ''
      });

      setForm({ exerciseId: '', dayNumber: Number(form.dayNumber) || 1, sortOrder: 1, sets: 3, reps: 12, notes: '' });
      await load();
    } catch (err) {
      setError(errorMessage(err, 'No se pudo agregar el ejercicio.'));
    } finally {
      setSaving(false);
    }
  };

  const remove = (row) => {
    setConfirm({
      title: 'Quitar ejercicio',
      message: `¿Quitar "${row.exerciseName}" del plan?`,
      action: async () => {
        await api.delete(`/routines/plan-exercises/${row.id}`);
        await load();
      }
    });
  };

  const runConfirm = async () => {
    if (!confirm) return;
    setConfirmLoading(true);
    try {
      await confirm.action();
      setConfirm(null);
    } catch (err) {
      setError(errorMessage(err, 'No se pudo completar la acción.'));
      setConfirm(null);
    } finally {
      setConfirmLoading(false);
    }
  };

  return (
    <Modal isOpen={!!plan} onClose={onClose} title={`Ejercicios · ${plan?.planName || ''}`} size="xl">
      {plan && (
        <div className="space-y-4">
          <form onSubmit={add} className="rounded-lg border border-slate-200 bg-slate-50 p-4">
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-6">
              <div className="sm:col-span-2">
                <Select
                  label="Ejercicio"
                  value={form.exerciseId}
                  onChange={(e) => setForm({ ...form, exerciseId: e.target.value })}
                  options={catalog.map((item) => ({
                    value: item.id,
                    label: `${item.exerciseName}${item.muscleGroup ? ` · ${item.muscleGroup}` : ''}`
                  }))}
                  placeholder="Selecciona"
                  required
                />
              </div>
              <Input
                label="Día"
                type="number"
                min="1"
                value={form.dayNumber}
                onChange={(e) => setForm({ ...form, dayNumber: e.target.value })}
              />
              <Input
                label="Orden"
                type="number"
                min="1"
                value={form.sortOrder}
                onChange={(e) => setForm({ ...form, sortOrder: e.target.value })}
              />
              <Input
                label="Series"
                type="number"
                min="0"
                value={form.sets}
                onChange={(e) => setForm({ ...form, sets: e.target.value })}
              />
              <Input
                label="Reps"
                type="number"
                min="0"
                value={form.reps}
                onChange={(e) => setForm({ ...form, reps: e.target.value })}
              />
            </div>
            <div className="mt-3 flex flex-col gap-2 sm:flex-row sm:items-end">
              <Input
                label="Notas"
                value={form.notes}
                onChange={(e) => setForm({ ...form, notes: e.target.value })}
                placeholder="Opcional"
                className="flex-1"
              />
              <Button type="submit" icon={Plus} loading={saving} disabled={!form.exerciseId}>
                Agregar
              </Button>
            </div>
          </form>

          {error && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <Table
            columns={[
              { header: 'Día', accessor: 'dayNumber' },
              { header: 'Orden', accessor: 'sortOrder' },
              {
                header: 'Ejercicio',
                accessor: 'exerciseName',
                render: (value, row) => (
                  <div>
                    <p className="font-medium text-slate-800">{value}</p>
                    <p className="text-xs text-slate-400">{row.muscleGroup || row.equipment || '—'}</p>
                  </div>
                ),
              },
              { header: 'Series', accessor: 'sets', render: (value) => value ?? '—' },
              { header: 'Reps', accessor: 'reps', render: (value) => value ?? '—' },
              { header: 'Notas', accessor: 'notes', render: (value) => value || '—' },
              {
                header: '',
                accessor: 'id',
                render: (_, row) => (
                  <div className="flex justify-end">
                    <Button variant="ghost" size="sm" icon={Trash2} onClick={() => remove(row)}>
                      Quitar
                    </Button>
                  </div>
                ),
              },
            ]}
            data={items}
            loading={loading}
            emptyText="Este plan no tiene ejercicios"
          />
        </div>
      )}

      <ConfirmDialog
        isOpen={!!confirm}
        onClose={() => setConfirm(null)}
        onConfirm={runConfirm}
        loading={confirmLoading}
        title={confirm?.title || 'Confirmar'}
        message={confirm?.message || '¿Deseas continuar?'}
        confirmLabel="Quitar"
        variant="danger"
      />
    </Modal>
  );
}
