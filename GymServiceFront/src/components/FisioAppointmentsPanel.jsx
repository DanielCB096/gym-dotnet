import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  AlertCircle,
  CalendarDays,
  CalendarPlus,
  CheckCircle2,
  Pencil,
  Trash2,
  UserX,
  XCircle,
} from 'lucide-react';
import api from '../services/api';
import Modal from './ui/Modal';
import Button from './ui/Button';
import Input from './ui/Input';
import Select from './ui/Select';
import Table from './ui/Table';
import Badge from './ui/Badge';
import ConfirmDialog from './ui/ConfirmDialog';
import { formatDateTime } from '../utils/format';

const statusVariant = {
  scheduled: 'info',
  completed: 'success',
  cancelled: 'neutral',
  no_show: 'warning',
};

const statusLabel = {
  scheduled: 'Programada',
  completed: 'Completada',
  cancelled: 'Cancelada',
  no_show: 'No asistió',
};

const statusOptions = [
  { value: 'scheduled', label: 'Programada' },
  { value: 'completed', label: 'Completada' },
  { value: 'cancelled', label: 'Cancelada' },
  { value: 'no_show', label: 'No asistió' },
];

const errorMessage = (err, fallback) => err?.response?.data?.message || fallback;

const pad = (value) => String(value).padStart(2, '0');

const toLocalInput = (value) => {
  const date = value ? new Date(value) : new Date();
  if (Number.isNaN(date.getTime())) return '';
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
};

const defaultAppointmentDate = () => {
  const date = new Date();
  date.setDate(date.getDate() + 1);
  date.setHours(10, 0, 0, 0);
  return toLocalInput(date);
};

export default function FisioAppointmentsPanel({ initialMemberId = '' }) {
  const [members, setMembers] = useState([]);
  const [packages, setPackages] = useState([]);

  const [filterMember, setFilterMember] = useState(initialMemberId ? String(initialMemberId) : '');
  const [filterStatus, setFilterStatus] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [page, setPage] = useState(1);

  const [appointments, setAppointments] = useState([]);
  const [pagination, setPagination] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState(null);

  const [modal, setModal] = useState(null); // { mode: 'create' | 'edit', row? }
  const [form, setForm] = useState({});
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);

  const [confirm, setConfirm] = useState(null);
  const [confirmLoading, setConfirmLoading] = useState(false);

  const notify = (type, message) => {
    setNotice({ type, message });
    window.clearTimeout(notify.timer);
    notify.timer = window.setTimeout(() => setNotice(null), 4500);
  };

  const loadCatalogs = useCallback(async () => {
    const [membersResponse, packagesResponse] = await Promise.allSettled([
      api.get('/members', { params: { limit: 500 } }),
      api.get('/fisio/packages'),
    ]);

    if (membersResponse.status === 'fulfilled') {
      setMembers((membersResponse.value.data?.data || []).filter((item) => item.status === 'active'));
    }
    if (packagesResponse.status === 'fulfilled') {
      setPackages(packagesResponse.value.data?.data || []);
    }
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const params = { page, limit: 50 };
      if (filterMember) params.memberId = Number(filterMember);
      if (filterStatus) params.status = filterStatus;
      if (from) params.from = `${from}T00:00:00`;
      if (to) params.to = `${to}T23:59:59`;

      const response = await api.get('/fisio/appointments', { params });
      setAppointments(response.data?.data || []);
      setPagination(response.data?.pagination || null);
    } catch (err) {
      setAppointments([]);
      setPagination(null);
      setError(errorMessage(err, 'No se pudieron cargar las citas.'));
    } finally {
      setLoading(false);
    }
  }, [page, filterMember, filterStatus, from, to]);

  useEffect(() => {
    loadCatalogs();
  }, [loadCatalogs]);

  useEffect(() => {
    load();
  }, [load]);

  const memberOptions = useMemo(
    () => members.map((member) => ({
      value: member.id,
      label: `${member.firstName} ${member.lastName}${member.email ? ` · ${member.email}` : ''}`,
    })),
    [members],
  );

  const eligiblePackages = useMemo(() => {
    const memberId = Number(form.memberId) || 0;
    return packages.filter((item) => {
      if (!item.isActive) return false;
      if ((item.sessionsRemaining ?? 0) <= 0) return false;
      if (item.memberId && memberId && item.memberId !== memberId) return false;
      return true;
    });
  }, [packages, form.memberId]);

  const openCreate = () => {
    setForm({
      memberId: filterMember || '',
      packageId: '',
      appointmentDate: defaultAppointmentDate(),
      notes: '',
    });
    setFormError('');
    setModal({ mode: 'create' });
  };

  const openEdit = (row) => {
    setForm({
      memberId: row.memberId,
      packageId: row.packageId || '',
      appointmentDate: toLocalInput(row.appointmentDate),
      notes: row.notes || '',
    });
    setFormError('');
    setModal({ mode: 'edit', row });
  };

  const save = async (event) => {
    event.preventDefault();
    setSaving(true);
    setFormError('');

    try {
      const payload = {
        memberId: Number(form.memberId) || 0,
        packageId: Number(form.packageId) || 0,
        appointmentDate: form.appointmentDate,
        notes: form.notes || '',
      };

      if (modal.mode === 'edit') {
        await api.put(`/fisio/appointments/${modal.row.id}`, {
          appointmentDate: payload.appointmentDate,
          notes: payload.notes,
        });
        notify('success', 'Cita actualizada correctamente.');
      } else {
        const response = await api.post('/fisio/appointments', payload);
        notify('success', response.data?.message || 'Cita agendada correctamente.');
      }

      setModal(null);
      await Promise.all([load(), loadCatalogs()]);
    } catch (err) {
      setFormError(errorMessage(err, 'No se pudo guardar la cita.'));
    } finally {
      setSaving(false);
    }
  };

  const changeStatus = (row, status, title, message) => {
    setConfirm({
      title,
      message,
      variant: status === 'completed' ? 'primary' : 'danger',
      action: async () => {
        const response = await api.put(`/fisio/appointments/${row.id}/status`, { status });
        notify('success', response.data?.message || 'Cita actualizada.');
        await Promise.all([load(), loadCatalogs()]);
      },
    });
  };

  const remove = (row) => {
    setConfirm({
      title: 'Eliminar cita',
      message: 'La cita se borrará definitivamente. ¿Continuar?',
      variant: 'danger',
      action: async () => {
        await api.delete(`/fisio/appointments/${row.id}`);
        notify('success', 'Cita eliminada.');
        await load();
      },
    });
  };

  const runConfirm = async () => {
    if (!confirm) return;
    setConfirmLoading(true);
    try {
      await confirm.action();
      setConfirm(null);
    } catch (err) {
      notify('error', errorMessage(err, 'No se pudo completar la acción.'));
      setConfirm(null);
    } finally {
      setConfirmLoading(false);
    }
  };

  const columns = [
    {
      header: 'Fecha',
      accessor: 'appointmentDate',
      render: (value) => <span className="font-medium text-slate-800">{formatDateTime(value)}</span>,
    },
    {
      header: 'Socio',
      accessor: 'memberName',
      render: (value, row) => (
        <div>
          <p className="font-medium text-slate-800">{value}</p>
          <p className="text-xs text-slate-400">Socio #{row.memberId}</p>
        </div>
      ),
    },
    {
      header: 'Paquete',
      accessor: 'packageName',
      render: (value) => value || <span className="text-slate-400">Sin paquete</span>,
    },
    {
      header: 'Estado',
      accessor: 'status',
      render: (value) => (
        <Badge variant={statusVariant[value] || 'neutral'} dot>
          {statusLabel[value] || value}
        </Badge>
      ),
    },
    { header: 'Notas', accessor: 'notes', render: (value) => value || '—' },
    {
      header: '',
      accessor: 'id',
      render: (_, row) => (
        <div className="flex items-center justify-end gap-1">
          <Button
            variant="ghost"
            size="sm"
            icon={CheckCircle2}
            disabled={row.status !== 'scheduled'}
            onClick={() => changeStatus(
              row,
              'completed',
              'Completar cita',
              row.packageId
                ? 'Se descontará una sesión del paquete del socio. ¿Continuar?'
                : 'Se marcará la cita como completada. ¿Continuar?',
            )}
          >
            Completar
          </Button>
          <Button
            variant="ghost"
            size="sm"
            icon={UserX}
            disabled={row.status !== 'scheduled'}
            onClick={() => changeStatus(row, 'no_show', 'Marcar no asistió', 'La cita quedará como no asistida. ¿Continuar?')}
          >
            No asistió
          </Button>
          <Button
            variant="ghost"
            size="sm"
            icon={XCircle}
            disabled={row.status === 'cancelled' || row.status === 'completed'}
            onClick={() => changeStatus(row, 'cancelled', 'Cancelar cita', 'La cita quedará cancelada. ¿Continuar?')}
          >
            Cancelar
          </Button>
          <Button
            variant="ghost"
            size="sm"
            icon={Pencil}
            disabled={row.status !== 'scheduled'}
            onClick={() => openEdit(row)}
          >
            Reprogramar
          </Button>
          <Button
            variant="ghost"
            size="sm"
            icon={Trash2}
            disabled={row.status === 'completed'}
            onClick={() => remove(row)}
          >
            Eliminar
          </Button>
        </div>
      ),
    },
  ];

  const totalPages = pagination?.totalPages || 1;

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm lg:flex-row lg:items-end">
        <Select
          label="Socio"
          value={filterMember}
          onChange={(event) => { setFilterMember(event.target.value); setPage(1); }}
          options={memberOptions}
          placeholder="Todos"
        />
        <Select
          label="Estado"
          value={filterStatus}
          onChange={(event) => { setFilterStatus(event.target.value); setPage(1); }}
          options={statusOptions}
          placeholder="Todos"
        />
        <Input label="Desde" type="date" value={from} onChange={(event) => { setFrom(event.target.value); setPage(1); }} />
        <Input label="Hasta" type="date" value={to} onChange={(event) => { setTo(event.target.value); setPage(1); }} />
        <Button icon={CalendarPlus} onClick={openCreate} className="lg:mb-0.5">
          Agendar cita
        </Button>
      </div>

      {notice && (
        <div
          className={`flex items-start gap-2 rounded-lg border px-3 py-2 text-sm ${
            notice.type === 'error'
              ? 'border-danger-200 bg-danger-50 text-danger-700'
              : 'border-success-200 bg-success-50 text-success-700'
          }`}
        >
          {notice.type === 'error'
            ? <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
            : <CheckCircle2 className="h-4 w-4 mt-0.5 flex-shrink-0" />}
          <span>{notice.message}</span>
        </div>
      )}

      {error && (
        <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
          <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <Table
        columns={columns}
        data={appointments}
        loading={loading}
        emptyText="No hay citas para los filtros seleccionados"
        emptyIcon={CalendarDays}
      />

      {pagination && pagination.totalCount > 0 && (
        <div className="flex items-center justify-between text-sm text-slate-500">
          <span>
            {pagination.totalCount} cita(s) · página {pagination.page} de {totalPages}
          </span>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" disabled={!pagination.hasPrev} onClick={() => setPage((p) => Math.max(1, p - 1))}>
              Anterior
            </Button>
            <Button variant="outline" size="sm" disabled={!pagination.hasNext} onClick={() => setPage((p) => p + 1)}>
              Siguiente
            </Button>
          </div>
        </div>
      )}

      <Modal
        isOpen={!!modal}
        onClose={() => !saving && setModal(null)}
        title={modal?.mode === 'edit' ? 'Reprogramar cita' : 'Agendar cita de fisioterapia'}
        size="md"
      >
        <form onSubmit={save} className="space-y-4">
          {modal?.mode === 'create' && (
            <Select
              label="Socio"
              value={form.memberId || ''}
              onChange={(event) => setForm({ ...form, memberId: event.target.value, packageId: '' })}
              options={memberOptions}
              placeholder="Selecciona un socio"
              required
            />
          )}

          {modal?.mode === 'create' && (
            <Select
              label="Paquete"
              value={form.packageId || ''}
              onChange={(event) => setForm({ ...form, packageId: event.target.value })}
              options={eligiblePackages.map((item) => ({
                value: item.id,
                label: `${item.name} · ${item.sessionsRemaining} sesión(es)${item.memberId ? '' : ' · general'}`,
              }))}
              placeholder="Sin paquete"
            />
          )}

          <Input
            label="Fecha y hora"
            type="datetime-local"
            value={form.appointmentDate || ''}
            onChange={(event) => setForm({ ...form, appointmentDate: event.target.value })}
            required
          />

          <Input
            label="Notas"
            value={form.notes || ''}
            onChange={(event) => setForm({ ...form, notes: event.target.value })}
            placeholder="Motivo de la cita"
          />

          {formError && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{formError}</span>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={() => setModal(null)} disabled={saving}>
              Cancelar
            </Button>
            <Button type="submit" loading={saving} disabled={!form.memberId}>
              {modal?.mode === 'edit' ? 'Guardar cambios' : 'Agendar'}
            </Button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!confirm}
        onClose={() => setConfirm(null)}
        onConfirm={runConfirm}
        loading={confirmLoading}
        title={confirm?.title || 'Confirmar'}
        message={confirm?.message || '¿Deseas continuar?'}
        confirmLabel="Continuar"
        variant={confirm?.variant === 'danger' ? 'danger' : 'primary'}
      />
    </div>
  );
}
