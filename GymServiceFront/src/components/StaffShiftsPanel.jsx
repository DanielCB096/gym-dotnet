import { useCallback, useEffect, useState } from 'react';
import {
  AlertCircle,
  CalendarClock,
  CheckCircle2,
  Clock,
  LogIn,
  LogOut,
  Pencil,
  Plus,
  Trash2,
  UserX,
} from 'lucide-react';
import api from '../services/api';
import Badge from '../components/ui/Badge';
import Button from '../components/ui/Button';
import ConfirmDialog from '../components/ui/ConfirmDialog';
import Input from '../components/ui/Input';
import Modal from '../components/ui/Modal';
import Select from '../components/ui/Select';
import Table from '../components/ui/Table';
import { formatDate } from '../utils/format';

const errorMessage = (err, fallback) => err?.response?.data?.message || fallback;

const statusInfo = {
  scheduled: { variant: 'neutral', label: 'Programado' },
  open: { variant: 'info', label: 'En turno' },
  closed: { variant: 'success', label: 'Cerrado' },
  absent: { variant: 'danger', label: 'Ausencia' },
  cancelled: { variant: 'neutral', label: 'Cancelado' },
};

const dateValue = (date) => {
  const copy = new Date(date);
  const month = String(copy.getMonth() + 1).padStart(2, '0');
  const day = String(copy.getDate()).padStart(2, '0');
  return `${copy.getFullYear()}-${month}-${day}`;
};

const timeValue = (date) => {
  const copy = new Date(date);
  return `${String(copy.getHours()).padStart(2, '0')}:${String(copy.getMinutes()).padStart(2, '0')}`;
};

const addDays = (date, days) => {
  const copy = new Date(date);
  copy.setDate(copy.getDate() + days);
  return copy;
};

const buildDateTime = (date, time) => {
  if (!date || !time) return '';
  return `${date}T${time.length === 5 ? `${time}:00` : time}`;
};

const formatTime = (value) => {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return date.toLocaleTimeString('es-MX', { hour: '2-digit', minute: '2-digit' });
};

const formatDuration = (minutes) => {
  const total = Number(minutes) || 0;
  if (total < 0) return '—';
  const hours = Math.floor(total / 60);
  const rest = total % 60;
  if (hours === 0) return `${rest} min`;
  return rest === 0 ? `${hours} h` : `${hours} h ${rest} min`;
};

const emptyForm = () => ({
  id: null,
  staffId: '',
  date: dateValue(new Date()),
  start: '09:00',
  end: '17:00',
  notes: '',
});

export default function StaffShiftsPanel() {
  const [staff, setStaff] = useState([]);
  const [staffLoading, setStaffLoading] = useState(true);
  const [shifts, setShifts] = useState([]);
  const [summary, setSummary] = useState(null);
  const [filters, setFilters] = useState(() => ({
    staffId: '',
    from: dateValue(addDays(new Date(), -7)),
    to: dateValue(addDays(new Date(), 7)),
    status: '',
  }));
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [form, setForm] = useState(emptyForm);
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);
  const [confirm, setConfirm] = useState(null);
  const [confirmLoading, setConfirmLoading] = useState(false);

  const notify = (type, message) => {
    setNotice({ type, message });
    window.clearTimeout(notify.timer);
    notify.timer = window.setTimeout(() => setNotice(null), 4500);
  };

  const loadStaff = useCallback(async () => {
    setStaffLoading(true);
    try {
      const response = await api.get('/staff', { params: { limit: 200 } });
      setStaff(response.data?.data || []);
    } catch {
      setStaff([]);
    } finally {
      setStaffLoading(false);
    }
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const params = { limit: 200 };
      if (filters.staffId) params.staffId = Number(filters.staffId);
      if (filters.from) params.from = filters.from;
      if (filters.to) params.to = filters.to;
      if (filters.status) params.status = filters.status;

      const summaryParams = {};
      if (filters.from) summaryParams.from = filters.from;
      if (filters.to) summaryParams.to = filters.to;

      const [shiftsResponse, summaryResponse] = await Promise.all([
        api.get('/staff/shifts', { params }),
        api.get('/staff/shifts/summary', { params: summaryParams }),
      ]);

      setShifts(shiftsResponse.data?.data || []);
      setSummary(summaryResponse.data?.data || null);
    } catch (err) {
      setShifts([]);
      setSummary(null);
      setError(errorMessage(err, 'No se pudieron cargar los turnos.'));
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => { loadStaff(); }, [loadStaff]);
  useEffect(() => { load(); }, [load]);

  const staffOptions = staff.map((employee) => ({
    value: employee.id,
    label: `${employee.employeeName}${employee.position ? ` · ${employee.position}` : ''}`,
  }));

  const openCreate = () => {
    const next = emptyForm();
    if (staff.length === 1) next.staffId = staff[0].id;
    if (filters.staffId) next.staffId = filters.staffId;
    setForm(next);
    setFormError('');
    setModalOpen(true);
  };

  const openEdit = (shift) => {
    setForm({
      id: shift.id,
      staffId: shift.staffId,
      date: dateValue(shift.scheduledStart),
      start: timeValue(shift.scheduledStart),
      end: shift.scheduledEnd ? timeValue(shift.scheduledEnd) : '',
      notes: shift.notes || '',
    });
    setFormError('');
    setModalOpen(true);
  };

  const submit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setFormError('');
    try {
      const payload = {
        staffId: Number(form.staffId),
        shiftDate: form.date,
        scheduledStart: buildDateTime(form.date, form.start),
        scheduledEnd: form.end ? buildDateTime(form.date, form.end) : '',
        notes: form.notes,
      };

      if (form.id) {
        const response = await api.put(`/staff/shifts/${form.id}`, payload);
        notify('success', response.data?.message || 'Turno actualizado.');
      } else {
        const response = await api.post('/staff/shifts', payload);
        notify('success', response.data?.message || 'Turno programado.');
      }

      setModalOpen(false);
      await load();
    } catch (err) {
      setFormError(errorMessage(err, 'No se pudo guardar el turno.'));
    } finally {
      setSaving(false);
    }
  };

  const runAction = async (shift, action, message) => {
    try {
      const response = await api.post(`/staff/shifts/${shift.id}/${action}`);
      notify('success', response.data?.message || message);
      await load();
    } catch (err) {
      notify('error', errorMessage(err, 'No se pudo completar la acción.'));
    }
  };

  const confirmAction = (shift, action, title, message, variant = 'primary') => {
    setConfirm({
      title,
      message,
      variant,
      action: () => api.post(`/staff/shifts/${shift.id}/${action}`),
    });
  };

  const runConfirm = async () => {
    if (!confirm) return;
    setConfirmLoading(true);
    try {
      const response = await confirm.action();
      setConfirm(null);
      notify('success', response.data?.message || 'Acción completada.');
      await load();
    } catch (err) {
      notify('error', errorMessage(err, 'No se pudo completar la acción.'));
      setConfirm(null);
    } finally {
      setConfirmLoading(false);
    }
  };

  const cards = [
    { label: 'Programados', value: summary?.scheduledCount ?? 0 },
    { label: 'En turno', value: summary?.openCount ?? 0 },
    { label: 'Cerrados', value: summary?.closedCount ?? 0 },
    { label: 'Ausencias', value: summary?.absentCount ?? 0 },
    { label: 'Horas trabajadas', value: formatDuration(summary?.workedMinutes ?? 0) },
    { label: 'Retardos', value: summary?.lateCount ?? 0 },
  ];

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm lg:flex-row lg:items-end">
        <Select
          label="Empleado"
          value={filters.staffId}
          onChange={(event) => setFilters({ ...filters, staffId: event.target.value })}
          options={staffOptions}
          placeholder="Todos"
        />
        <Input
          label="Desde"
          type="date"
          value={filters.from}
          onChange={(event) => setFilters({ ...filters, from: event.target.value })}
        />
        <Input
          label="Hasta"
          type="date"
          value={filters.to}
          onChange={(event) => setFilters({ ...filters, to: event.target.value })}
        />
        <Select
          label="Estado"
          value={filters.status}
          onChange={(event) => setFilters({ ...filters, status: event.target.value })}
          options={Object.entries(statusInfo).map(([value, info]) => ({ value, label: info.label }))}
          placeholder="Todos"
        />
        <div className="flex gap-2 lg:ml-auto">
          <Button variant="outline" onClick={load}>Actualizar</Button>
          <Button icon={Plus} onClick={openCreate} disabled={staffLoading || staff.length === 0}>
            Nuevo turno
          </Button>
        </div>
      </div>

      {summary && (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
          {cards.map((card) => (
            <div key={card.label} className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{card.label}</p>
              <p className="mt-1 text-xl font-bold text-slate-800">{card.value}</p>
            </div>
          ))}
        </div>
      )}

      {notice && (
        <div className={`flex items-start gap-2 rounded-lg border px-3 py-2 text-sm ${
          notice.type === 'error'
            ? 'border-danger-200 bg-danger-50 text-danger-700'
            : 'border-success-200 bg-success-50 text-success-700'
        }`}>
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
        columns={[
          {
            header: 'Empleado',
            accessor: 'employeeName',
            render: (value, row) => (
              <div>
                <p className="font-medium text-slate-800">{value}</p>
                <p className="text-xs text-slate-400">{row.position || row.department || '—'}</p>
              </div>
            ),
          },
          {
            header: 'Fecha',
            accessor: 'shiftDate',
            render: (value) => formatDate(value),
          },
          {
            header: 'Horario',
            accessor: 'scheduledStart',
            render: (value, row) => (
              <span className="text-slate-700">
                {formatTime(value)}{row.scheduledEnd ? ` – ${formatTime(row.scheduledEnd)}` : ''}
              </span>
            ),
          },
          {
            header: 'Entrada',
            accessor: 'checkIn',
            render: (value) => formatTime(value),
          },
          {
            header: 'Salida',
            accessor: 'checkOut',
            render: (value) => formatTime(value),
          },
          {
            header: 'Trabajado',
            accessor: 'workedMinutes',
            render: (value, row) => (
              <div>
                <p className="text-slate-700">{row.checkIn ? formatDuration(value) : '—'}</p>
                {row.lateMinutes > 0 && (
                  <p className="text-xs text-warning-600">Retardo {formatDuration(row.lateMinutes)}</p>
                )}
              </div>
            ),
          },
          {
            header: 'Estado',
            accessor: 'status',
            render: (value) => {
              const info = statusInfo[value] || { variant: 'neutral', label: value };
              return <Badge variant={info.variant} dot>{info.label}</Badge>;
            },
          },
          {
            header: '',
            accessor: 'id',
            render: (_, row) => (
              <div className="flex items-center justify-end gap-1">
                {row.status === 'scheduled' && (
                  <>
                    <Button
                      variant="ghost"
                      size="sm"
                      icon={LogIn}
                      onClick={() => runAction(row, 'check-in', 'Entrada registrada.')}
                    >
                      Entrar
                    </Button>
                    <Button variant="ghost" size="sm" icon={Pencil} onClick={() => openEdit(row)}>
                      Editar
                    </Button>
                  </>
                )}
                {row.status === 'open' && (
                  <Button
                    variant="ghost"
                    size="sm"
                    icon={LogOut}
                    onClick={() => runAction(row, 'check-out', 'Salida registrada.')}
                  >
                    Salir
                  </Button>
                )}
                {row.status === 'scheduled' && new Date(row.scheduledStart) <= new Date() && (
                  <Button
                    variant="ghost"
                    size="sm"
                    icon={UserX}
                    onClick={() => confirmAction(
                      row,
                      'absent',
                      'Marcar ausencia',
                      'El turno se marcará como ausencia del empleado. ¿Continuar?',
                      'danger',
                    )}
                  >
                    Falta
                  </Button>
                )}
                {(row.status === 'scheduled' || row.status === 'absent') && (
                  <Button
                    variant="ghost"
                    size="sm"
                    icon={Trash2}
                    onClick={() => confirmAction(
                      row,
                      'cancel',
                      'Cancelar turno',
                      'El turno quedará cancelado. ¿Continuar?',
                      'danger',
                    )}
                  >
                    Cancelar
                  </Button>
                )}
              </div>
            ),
          },
        ]}
        data={shifts}
        loading={loading}
        emptyText="No hay turnos en el rango seleccionado"
        emptyIcon={CalendarClock}
      />

      <Modal
        isOpen={modalOpen}
        onClose={() => !saving && setModalOpen(false)}
        title={form.id ? 'Editar turno' : 'Nuevo turno'}
        size="md"
      >
        <form onSubmit={submit} className="space-y-4">
          <Select
            label="Empleado"
            value={form.staffId}
            onChange={(event) => setForm({ ...form, staffId: event.target.value })}
            options={staffOptions}
            placeholder="Selecciona un empleado"
            required
            disabled={!!form.id}
          />
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <Input
              label="Fecha"
              type="date"
              value={form.date}
              onChange={(event) => setForm({ ...form, date: event.target.value })}
              required
            />
            <Input
              label="Inicio"
              type="time"
              value={form.start}
              onChange={(event) => setForm({ ...form, start: event.target.value })}
              required
            />
            <Input
              label="Fin"
              type="time"
              value={form.end}
              onChange={(event) => setForm({ ...form, end: event.target.value })}
            />
          </div>
          <Input
            label="Notas"
            value={form.notes}
            onChange={(event) => setForm({ ...form, notes: event.target.value })}
            placeholder="Turno, área o indicaciones"
          />

          {formError && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{formError}</span>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={() => setModalOpen(false)} disabled={saving}>Cancelar</Button>
            <Button type="submit" icon={Clock} loading={saving} disabled={!form.staffId || !form.start}>
              {form.id ? 'Guardar cambios' : 'Programar turno'}
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
