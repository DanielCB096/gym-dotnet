import { useCallback, useEffect, useState } from 'react';
import { AlertCircle, CheckCircle2, Plus, XCircle, Printer } from 'lucide-react';
import api from '../services/api';
import Button from './ui/Button';
import Select from './ui/Select';
import Table from './ui/Table';
import Badge from './ui/Badge';
import RoutinePrintPreview from './RoutinePrintPreview';
import { formatDateTime } from '../utils/format';

const errorMessage = (err, fallback) => err?.response?.data?.message || fallback;

export default function RoutineAssignmentsPanel() {
  const [members, setMembers] = useState([]);
  const [plans, setPlans] = useState([]);
  const [assignments, setAssignments] = useState([]);
  const [filterMember, setFilterMember] = useState('');
  const [form, setForm] = useState({ memberId: '', planId: '' });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [printingId, setPrintingId] = useState(null);
  const [printData, setPrintData] = useState(null);
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
      api.get('/routines'),
    ]).then(([membersResponse, plansResponse]) => {
      if (membersResponse.status === 'fulfilled') {
        setMembers((membersResponse.value.data?.data || []).filter((m) => m.status === 'active'));
      }
      if (plansResponse.status === 'fulfilled') {
        setPlans(plansResponse.value.data?.data || []);
      }
    });
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const params = filterMember ? { memberId: Number(filterMember) } : undefined;
      const response = await api.get('/routines/assignments', { params });
      setAssignments(response.data?.data || []);
    } catch (err) {
      setAssignments([]);
      setError(errorMessage(err, 'No se pudieron cargar las asignaciones.'));
    } finally {
      setLoading(false);
    }
  }, [filterMember]);

  useEffect(() => { load(); }, [load]);

  const assign = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');

    try {
      await api.post('/routines/assignments', {
        memberId: Number(form.memberId),
        planId: Number(form.planId)
      });
      notify('success', 'Rutina asignada correctamente.');
      setForm({ memberId: form.memberId, planId: '' });
      await load();
    } catch (err) {
      setError(errorMessage(err, 'No se pudo asignar la rutina.'));
    } finally {
      setSaving(false);
    }
  };

  const deactivate = async (row) => {
    try {
      await api.delete(`/routines/assignments/${row.id}`);
      notify('success', 'Asignación desactivada.');
      await load();
    } catch (err) {
      setError(errorMessage(err, 'No se pudo desactivar la asignación.'));
    }
  };

  const openPrint = async (row) => {
    setPrintingId(row.id);
    setError('');
    try {
      const response = await api.get(`/routines/${row.planId}/exercises`);
      const plan = plans.find((item) => Number(item.id) === Number(row.planId)) || null;
      setPrintData({
        assignment: row,
        plan,
        exercises: response.data?.data || []
      });
    } catch (err) {
      setError(errorMessage(err, 'No se pudo cargar la rutina para imprimir.'));
    } finally {
      setPrintingId(null);
    }
  };

  return (
    <div className="space-y-4">
      <form onSubmit={assign} className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-4">
          <Select
            label="Socio"
            value={form.memberId}
            onChange={(e) => setForm({ ...form, memberId: e.target.value })}
            options={members.map((m) => ({ value: m.id, label: `${m.firstName} ${m.lastName}` }))}
            placeholder="Selecciona un socio"
            required
          />
          <Select
            label="Rutina"
            value={form.planId}
            onChange={(e) => setForm({ ...form, planId: e.target.value })}
            options={plans.map((p) => ({ value: p.id, label: p.planName }))}
            placeholder="Selecciona una rutina"
            required
          />
          <div className="flex items-end sm:col-span-2">
            <Button type="submit" icon={Plus} loading={saving} disabled={!form.memberId || !form.planId}>
              Asignar rutina
            </Button>
          </div>
        </div>
        <p className="mt-2 text-xs text-slate-400">
          Al asignar una rutina se desactiva la asignación activa anterior del socio.
        </p>
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
          { header: 'Socio', accessor: 'memberName' },
          { header: 'Rutina', accessor: 'planName' },
          { header: 'Asignada', accessor: 'assignedAt', render: (value) => formatDateTime(value) },
          {
            header: 'Estado',
            accessor: 'isActive',
            render: (value) => (
              <Badge variant={value ? 'success' : 'neutral'} dot>
                {value ? 'Activa' : 'Inactiva'}
              </Badge>
            ),
          },
          {
            header: '',
            accessor: 'id',
            render: (_, row) => (
              <div className="flex justify-end gap-1">
                <Button
                  variant="ghost"
                  size="sm"
                  icon={Printer}
                  loading={printingId === row.id}
                  onClick={() => openPrint(row)}
                >
                  Imprimir
                </Button>
                <Button variant="ghost" size="sm" icon={XCircle} disabled={!row.isActive} onClick={() => deactivate(row)}>
                  Desactivar
                </Button>
              </div>
            ),
          },
        ]}
        data={assignments}
        loading={loading}
        emptyText="No hay rutinas asignadas"
      />

      <RoutinePrintPreview data={printData} onClose={() => setPrintData(null)} />
    </div>
  );
}
