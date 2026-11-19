import { useCallback, useEffect, useState } from 'react';
import { AlertCircle, CheckCircle2, Plus, Trash2 } from 'lucide-react';
import api from '../services/api';
import Button from './ui/Button';
import Input from './ui/Input';
import Select from './ui/Select';
import Table from './ui/Table';
import { formatDateTime, humanize } from '../utils/format';

const errorMessage = (err, fallback) => err?.response?.data?.message || fallback;

const pad = (value) => String(value).padStart(2, '0');

const toLocalInput = (value) => {
  const date = value ? new Date(value) : new Date();
  if (Number.isNaN(date.getTime())) return '';
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
};

const measurementTypes = [
  { value: 'weight', label: 'Peso', unit: 'kg' },
  { value: 'body_fat', label: 'Grasa corporal', unit: '%' },
  { value: 'chest', label: 'Pecho', unit: 'cm' },
  { value: 'waist', label: 'Cintura', unit: 'cm' },
  { value: 'hip', label: 'Cadera', unit: 'cm' },
  { value: 'arm', label: 'Brazo', unit: 'cm' },
  { value: 'thigh', label: 'Muslo', unit: 'cm' },
  { value: 'calf', label: 'Pantorrilla', unit: 'cm' },
  { value: 'height', label: 'Estatura', unit: 'cm' },
];

export default function RoutineMeasurementsPanel() {
  const [members, setMembers] = useState([]);
  const [measurements, setMeasurements] = useState([]);
  const [filterMember, setFilterMember] = useState('');
  const [form, setForm] = useState({
    memberId: '',
    measurementType: 'weight',
    value: '',
    unit: 'kg',
    measuredAt: toLocalInput()
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
    api.get('/members', { params: { limit: 500 } })
      .then((response) => {
        setMembers((response.data?.data || []).filter((m) => m.status === 'active'));
      })
      .catch(() => setMembers([]));
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const params = {};
      if (filterMember) params.memberId = Number(filterMember);
      const response = await api.get('/routines/measurements', { params });
      setMeasurements(response.data?.data || []);
    } catch (err) {
      setMeasurements([]);
      setError(errorMessage(err, 'No se pudieron cargar las medidas.'));
    } finally {
      setLoading(false);
    }
  }, [filterMember]);

  useEffect(() => { load(); }, [load]);

  const changeType = (value) => {
    const info = measurementTypes.find((item) => item.value === value);
    setForm({ ...form, measurementType: value, unit: info?.unit || 'cm' });
  };

  const create = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');

    try {
      await api.post('/routines/measurements', {
        memberId: Number(form.memberId),
        measurementType: form.measurementType,
        value: Number(form.value),
        unit: form.unit,
        measuredAt: form.measuredAt || undefined
      });

      notify('success', 'Medida registrada.');
      setForm({ ...form, value: '', measuredAt: toLocalInput() });
      await load();
    } catch (err) {
      setError(errorMessage(err, 'No se pudo registrar la medida.'));
    } finally {
      setSaving(false);
    }
  };

  const remove = async (row) => {
    try {
      await api.delete(`/routines/measurements/${row.id}`);
      notify('success', 'Medida eliminada.');
      await load();
    } catch (err) {
      setError(errorMessage(err, 'No se pudo eliminar la medida.'));
    }
  };

  return (
    <div className="space-y-4">
      <form onSubmit={create} className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-6">
          <Select
            label="Socio"
            value={form.memberId}
            onChange={(e) => setForm({ ...form, memberId: e.target.value })}
            options={members.map((m) => ({ value: m.id, label: `${m.firstName} ${m.lastName}` }))}
            placeholder="Selecciona"
            required
          />
          <Select
            label="Tipo"
            value={form.measurementType}
            onChange={(e) => changeType(e.target.value)}
            options={measurementTypes.map((item) => ({ value: item.value, label: item.label }))}
          />
          <Input
            label="Valor"
            type="number"
            step="0.01"
            value={form.value}
            onChange={(e) => setForm({ ...form, value: e.target.value })}
            required
          />
          <Input
            label="Unidad"
            value={form.unit}
            onChange={(e) => setForm({ ...form, unit: e.target.value })}
          />
          <Input
            label="Fecha"
            type="datetime-local"
            value={form.measuredAt}
            onChange={(e) => setForm({ ...form, measuredAt: e.target.value })}
          />
          <div className="flex items-end">
            <Button type="submit" icon={Plus} loading={saving} disabled={!form.memberId || form.value === ''}>
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
          { header: 'Fecha', accessor: 'measuredAt', render: (value) => formatDateTime(value) },
          { header: 'Socio', accessor: 'memberName' },
          {
            header: 'Medida',
            accessor: 'measurementType',
            render: (value) => {
              const info = measurementTypes.find((item) => item.value === value);
              return info?.label || humanize(value);
            },
          },
          {
            header: 'Valor',
            accessor: 'value',
            render: (value, row) => `${value} ${row.unit || ''}`,
          },
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
        data={measurements}
        loading={loading}
        emptyText="No hay medidas registradas"
      />
    </div>
  );
}
