import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  AlertCircle,
  CalendarDays,
  CheckCircle2,
  Pencil,
  Plus,
  Trash2,
  UserPlus,
  Users,
  XCircle,
} from 'lucide-react';
import api from '../services/api';
import Table from './ui/Table';
import Modal from './ui/Modal';
import Button from './ui/Button';
import Input from './ui/Input';
import Select from './ui/Select';
import Badge from './ui/Badge';
import ConfirmDialog from './ui/ConfirmDialog';
import { formatDateTime } from '../utils/format';

const sessionStatusVariant = {
  scheduled: 'success',
  cancelled: 'danger',
  completed: 'neutral',
};

const reservationStatusVariant = {
  reserved: 'info',
  waitlist: 'warning',
  attended: 'success',
  cancelled: 'neutral',
};

const reservationStatusLabel = {
  reserved: 'Confirmada',
  waitlist: 'En espera',
  attended: 'AsistiÃ³',
  cancelled: 'Cancelada',
};

const pad = (value) => String(value).padStart(2, '0');

const toLocalInput = (value) => {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
};

const todayInput = () => {
  const now = new Date();
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
};

const addDaysInput = (days) => {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
};

const errorMessage = (err, fallback) => err?.response?.data?.message || fallback;

export default function ClassSessionsPanel() {
  const [classTypes, setClassTypes] = useState([]);
  const [staff, setStaff] = useState([]);
  const [members, setMembers] = useState([]);

  const [from, setFrom] = useState(todayInput());
  const [to, setTo] = useState(addDaysInput(14));
  const [classTypeFilter, setClassTypeFilter] = useState('');

  const [sessions, setSessions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [sessionModal, setSessionModal] = useState(null); // { mode: 'create'|'edit', id? }
  const [sessionForm, setSessionForm] = useState({});
  const [sessionError, setSessionError] = useState('');
  const [savingSession, setSavingSession] = useState(false);

  const [reservationsFor, setReservationsFor] = useState(null); // session row
  const [reservations, setReservations] = useState([]);
  const [reservationsLoading, setReservationsLoading] = useState(false);
  const [reservationsError, setReservationsError] = useState('');
  const [newMemberId, setNewMemberId] = useState('');
  const [addingReservation, setAddingReservation] = useState(false);

  const [confirm, setConfirm] = useState(null); // { title, message, action, variant }
  const [confirmLoading, setConfirmLoading] = useState(false);
  const [notice, setNotice] = useState(null);

  const notify = (type, message) => {
    setNotice({ type, message });
    window.clearTimeout(notify.timer);
    notify.timer = window.setTimeout(() => setNotice(null), 4500);
  };

  const loadCatalogs = useCallback(async () => {
    const [typesResponse, staffResponse, membersResponse] = await Promise.allSettled([
      api.get('/classes'),
      api.get('/staff', { params: { limit: 500 } }),
      api.get('/members', { params: { limit: 500 } }),
    ]);

    if (typesResponse.status === 'fulfilled') {
      setClassTypes(typesResponse.value.data?.data || []);
    }
    if (staffResponse.status === 'fulfilled') {
      setStaff((staffResponse.value.data?.data || []).filter((item) => item.isActive !== false));
    }
    if (membersResponse.status === 'fulfilled') {
      setMembers((membersResponse.value.data?.data || []).filter((item) => item.status === 'active'));
    }
  }, []);

  const loadSessions = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const params = {
        from: from ? `${from}T00:00:00` : undefined,
        to: to ? `${to}T23:59:59` : undefined,
        limit: 200,
      };
      if (classTypeFilter) params.classTypeId = classTypeFilter;

      const response = await api.get('/classes/sessions', { params });
      setSessions(response.data?.data || []);
    } catch (err) {
      setSessions([]);
      setError(errorMessage(err, 'No se pudieron cargar las sesiones.'));
    } finally {
      setLoading(false);
    }
  }, [from, to, classTypeFilter]);

  useEffect(() => {
    loadCatalogs();
  }, [loadCatalogs]);

  useEffect(() => {
    loadSessions();
  }, [loadSessions]);

  const classTypeOptions = useMemo(
    () => classTypes.map((type) => ({
      value: type.id,
      label: `${type.typeName} Â· ${type.durationMinutes} min Â· mÃ¡x. ${type.maxParticipants}`,
    })),
    [classTypes],
  );

  const staffOptions = useMemo(
    () => staff.map((item) => ({ value: item.id, label: item.employeeName || `Personal ${item.id}` })),
    [staff],
  );

  const memberOptions = useMemo(
    () => members.map((member) => ({
      value: member.id,
      label: `${member.firstName} ${member.lastName}${member.email ? ` Â· ${member.email}` : ''}`,
    })),
    [members],
  );

  const openCreate = () => {
    const start = new Date();
    start.setDate(start.getDate() + 1);
    start.setHours(9, 0, 0, 0);

    setSessionForm({
      classTypeId: classTypeFilter || '',
      instructorId: '',
      room: '',
      startTime: toLocalInput(start),
      endTime: toLocalInput(new Date(start.getTime() + 60 * 60000)),
    });
    setSessionError('');
    setSessionModal({ mode: 'create' });
  };

  const openEdit = (row) => {
    setSessionForm({
      classTypeId: row.classTypeId,
      instructorId: row.instructorId || '',
      room: row.room || '',
      startTime: toLocalInput(row.startTime),
      endTime: toLocalInput(row.endTime),
    });
    setSessionError('');
    setSessionModal({ mode: 'edit', id: row.id });
  };

  const handleClassTypeChange = (value) => {
    const type = classTypes.find((item) => String(item.id) === String(value));
    const next = { ...sessionForm, classTypeId: value };

    if (type && next.startTime) {
      const start = new Date(next.startTime);
      if (!Number.isNaN(start.getTime())) {
        next.endTime = toLocalInput(new Date(start.getTime() + (type.durationMinutes || 60) * 60000));
      }
    }

    setSessionForm(next);
  };

  const saveSession = async (event) => {
    event.preventDefault();
    setSavingSession(true);
    setSessionError('');

    try {
      const payload = {
        classTypeId: Number(sessionForm.classTypeId) || 0,
        instructorId: Number(sessionForm.instructorId) || 0,
        room: sessionForm.room || '',
        startTime: sessionForm.startTime,
        endTime: sessionForm.endTime,
      };

      if (sessionModal.mode === 'edit') {
        await api.put(`/classes/sessions/${sessionModal.id}`, payload);
        notify('success', 'SesiÃ³n actualizada correctamente.');
      } else {
        await api.post('/classes/sessions', payload);
        notify('success', 'SesiÃ³n creada correctamente.');
      }

      setSessionModal(null);
      await loadSessions();
    } catch (err) {
      setSessionError(errorMessage(err, 'No se pudo guardar la sesiÃ³n.'));
    } finally {
      setSavingSession(false);
    }
  };

  const askCancelSession = (row) => {
    setConfirm({
      title: 'Cancelar sesiÃ³n',
      message: `Se cancelarÃ¡ la sesiÃ³n y sus reservas activas. Â¿Continuar?`,
      variant: 'danger',
      action: async () => {
        const response = await api.post(`/classes/sessions/${row.id}/cancel`);
        notify('success', response.data?.message || 'SesiÃ³n cancelada.');
        await loadSessions();
      },
    });
  };

  const askDeleteSession = (row) => {
    setConfirm({
      title: 'Eliminar sesiÃ³n',
      message: 'Solo se puede eliminar una sesiÃ³n sin reservas. Â¿Continuar?',
      variant: 'danger',
      action: async () => {
        await api.delete(`/classes/sessions/${row.id}`);
        notify('success', 'SesiÃ³n eliminada.');
        await loadSessions();
      },
    });
  };

  const openReservations = async (row) => {
    setReservationsFor(row);
    setNewMemberId('');
    await loadReservations(row.id);
  };

  const loadReservations = async (sessionId) => {
    setReservationsLoading(true);
    setReservationsError('');
    try {
      const response = await api.get(`/classes/sessions/${sessionId}/reservations`);
      setReservations(response.data?.data || []);
    } catch (err) {
      setReservations([]);
      setReservationsError(errorMessage(err, 'No se pudieron cargar las reservas.'));
    } finally {
      setReservationsLoading(false);
    }
  };

  const addReservation = async () => {
    if (!reservationsFor || !newMemberId) return;

    setAddingReservation(true);
    setReservationsError('');

    try {
      const response = await api.post(`/classes/sessions/${reservationsFor.id}/reservations`, {
        memberId: Number(newMemberId),
      });

      notify(response.data?.waitlisted ? 'warning' : 'success', response.data?.message || 'Reserva registrada.');
      setNewMemberId('');
      await Promise.all([loadReservations(reservationsFor.id), loadSessions()]);
    } catch (err) {
      setReservationsError(errorMessage(err, 'No se pudo registrar la reserva.'));
    } finally {
      setAddingReservation(false);
    }
  };

  const cancelReservation = (reservation) => {
    setConfirm({
      title: 'Cancelar reserva',
      message: 'Si hay lista de espera, el siguiente socio ocuparÃ¡ el lugar. Â¿Continuar?',
      variant: 'danger',
      action: async () => {
        const response = await api.delete(`/classes/reservations/${reservation.id}`);
        notify('success', response.data?.message || 'Reserva cancelada.');
        await Promise.all([loadReservations(reservationsFor.id), loadSessions()]);
      },
    });
  };

  const toggleAttendance = async (reservation, attended) => {
    try {
      await api.put(`/classes/reservations/${reservation.id}/attendance`, { attended });
      await loadReservations(reservationsFor.id);
    } catch (err) {
      setReservationsError(errorMessage(err, 'No se pudo registrar la asistencia.'));
    }
  };

  const runConfirm = async () => {
    if (!confirm) return;
    setConfirmLoading(true);
    try {
      await confirm.action();
      setConfirm(null);
    } catch (err) {
      notify('error', errorMessage(err, 'No se pudo completar la acciÃ³n.'));
      setConfirm(null);
    } finally {
      setConfirmLoading(false);
    }
  };

  const columns = [
    {
      header: 'Inicio',
      accessor: 'startTime',
      render: (value) => <span className="font-medium text-slate-800">{formatDateTime(value)}</span>,
    },
    {
      header: 'Clase',
      accessor: 'className',
      render: (value, row) => (
        <div>
          <p className="font-medium text-slate-800">{value}</p>
          <p className="text-xs text-slate-400">{row.room || 'Sin salÃ³n'}</p>
        </div>
      ),
    },
    {
      header: 'Instructor',
      accessor: 'instructorName',
      render: (value) => value || 'â€”',
    },
    {
      header: 'Cupo',
      accessor: 'reserved',
      render: (value, row) => (
        <div className="min-w-[110px]">
          <div className="flex items-center justify-between text-xs text-slate-500">
            <span>{value}/{row.capacity}</span>
            <span>{row.available} libres</span>
          </div>
          <div className="mt-1 h-1.5 w-full overflow-hidden rounded-full bg-slate-100">
            <div
              className={`h-full ${value >= row.capacity ? 'bg-warning-500' : 'bg-brand-500'}`}
              style={{ width: `${row.capacity > 0 ? Math.min(100, (value / row.capacity) * 100) : 0}%` }}
            />
          </div>
        </div>
      ),
    },
    {
      header: 'Estado',
      accessor: 'status',
      render: (value) => (
        <Badge variant={sessionStatusVariant[value] || 'neutral'} dot>
          {value === 'scheduled' ? 'Programada' : value === 'cancelled' ? 'Cancelada' : value}
        </Badge>
      ),
    },
    {
      header: '',
      accessor: 'id',
      render: (_, row) => (
        <div className="flex items-center justify-end gap-1">
          <Button variant="ghost" size="sm" icon={Users} onClick={() => openReservations(row)}>
            Reservas
          </Button>
          <Button
            variant="ghost"
            size="sm"
            icon={Pencil}
            disabled={row.status === 'cancelled'}
            onClick={() => openEdit(row)}
          >
            Editar
          </Button>
          <Button
            variant="ghost"
            size="sm"
            icon={XCircle}
            disabled={row.status === 'cancelled'}
            onClick={() => askCancelSession(row)}
          >
            Cancelar
          </Button>
          <Button variant="ghost" size="sm" icon={Trash2} onClick={() => askDeleteSession(row)}>
            Eliminar
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div>
      <div className="mb-4 flex flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:flex-row sm:items-end">
        <Input label="Desde" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        <Input label="Hasta" type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        <Select
          label="Tipo de clase"
          value={classTypeFilter}
          onChange={(e) => setClassTypeFilter(e.target.value)}
          options={classTypeOptions}
          placeholder="Todas"
        />
        <Button icon={Plus} onClick={openCreate} className="sm:mb-0.5">
          Nueva sesiÃ³n
        </Button>
      </div>

      {error && (
        <div className="mb-4 flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
          <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {notice && (
        <div
          className={`mb-4 flex items-start gap-2 rounded-lg border px-3 py-2 text-sm ${
            notice.type === 'error'
              ? 'border-danger-200 bg-danger-50 text-danger-700'
              : notice.type === 'warning'
                ? 'border-warning-200 bg-warning-50 text-warning-700'
                : 'border-success-200 bg-success-50 text-success-700'
          }`}
        >
          {notice.type === 'success' ? (
            <CheckCircle2 className="h-4 w-4 mt-0.5 flex-shrink-0" />
          ) : (
            <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
          )}
          <span>{notice.message}</span>
        </div>
      )}

      <Table
        columns={columns}
        data={sessions}
        loading={loading}
        emptyText="No hay sesiones en el rango seleccionado"
        emptyIcon={CalendarDays}
        emptyAction={<Button icon={Plus} size="sm" onClick={openCreate}>Nueva sesiÃ³n</Button>}
      />

      {/* Modal crear/editar sesiÃ³n */}
      <Modal
        isOpen={!!sessionModal}
        onClose={() => setSessionModal(null)}
        title={sessionModal?.mode === 'edit' ? 'Editar sesiÃ³n' : 'Nueva sesiÃ³n'}
        size="lg"
      >
        <form onSubmit={saveSession} className="space-y-4">
          <Select
            label="Tipo de clase"
            value={sessionForm.classTypeId || ''}
            onChange={(e) => handleClassTypeChange(e.target.value)}
            options={classTypeOptions}
            placeholder="Selecciona una clase"
            required
          />
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Select
              label="Instructor"
              value={sessionForm.instructorId || ''}
              onChange={(e) => setSessionForm({ ...sessionForm, instructorId: e.target.value })}
              options={staffOptions}
              placeholder="Sin instructor"
            />
            <Input
              label="SalÃ³n"
              value={sessionForm.room || ''}
              onChange={(e) => setSessionForm({ ...sessionForm, room: e.target.value })}
              placeholder="Ej. Sala 1"
            />
            <Input
              label="Inicio"
              type="datetime-local"
              value={sessionForm.startTime || ''}
              onChange={(e) => setSessionForm({ ...sessionForm, startTime: e.target.value })}
              required
            />
            <Input
              label="Fin"
              type="datetime-local"
              value={sessionForm.endTime || ''}
              onChange={(e) => setSessionForm({ ...sessionForm, endTime: e.target.value })}
              required
            />
          </div>

          {sessionError && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{sessionError}</span>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={() => setSessionModal(null)} disabled={savingSession}>
              Cancelar
            </Button>
            <Button type="submit" loading={savingSession}>
              {sessionModal?.mode === 'edit' ? 'Guardar cambios' : 'Crear sesiÃ³n'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal reservas */}
      <Modal
        isOpen={!!reservationsFor}
        onClose={() => setReservationsFor(null)}
        title={`Reservas Â· ${reservationsFor?.className || ''}`}
        size="xl"
      >
        {reservationsFor && (
          <div className="space-y-4">
            <div className="flex flex-wrap items-center gap-2 rounded-lg bg-slate-50 px-4 py-3 text-sm text-slate-600">
              <CalendarDays className="h-4 w-4" />
              <span>{formatDateTime(reservationsFor.startTime)}</span>
              <span className="text-slate-300">Â·</span>
              <span>{reservationsFor.room || 'Sin salÃ³n'}</span>
              <span className="text-slate-300">Â·</span>
              <span>
                {reservationsFor.reserved}/{reservationsFor.capacity} lugares
              </span>
            </div>

            <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
              <Select
                label="Agregar socio"
                value={newMemberId}
                onChange={(e) => setNewMemberId(e.target.value)}
                options={memberOptions}
                placeholder="Selecciona un socio"
                className="flex-1"
              />
              <Button
                icon={UserPlus}
                onClick={addReservation}
                loading={addingReservation}
                disabled={!newMemberId || reservationsFor.status === 'cancelled'}
                className="sm:mb-0.5"
              >
                Reservar
              </Button>
            </div>

            {reservationsError && (
              <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
                <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
                <span>{reservationsError}</span>
              </div>
            )}

            <Table
              columns={[
                {
                  header: 'Socio',
                  accessor: 'memberName',
                  render: (value, row) => (
                    <div>
                      <p className="font-medium text-slate-800">{value || `Socio ${row.memberId}`}</p>
                      <p className="text-xs text-slate-400">{row.email || 'â€”'}</p>
                    </div>
                  ),
                },
                {
                  header: 'Estado',
                  accessor: 'status',
                  render: (value, row) => (
                    <div className="flex items-center gap-2">
                      <Badge variant={reservationStatusVariant[value] || 'neutral'} dot>
                        {reservationStatusLabel[value] || value}
                      </Badge>
                      {value === 'waitlist' && row.waitlistPosition ? (
                        <span className="text-xs text-slate-400">#{row.waitlistPosition}</span>
                      ) : null}
                    </div>
                  ),
                },
                {
                  header: 'Reservada',
                  accessor: 'reservedAt',
                  render: (value) => formatDateTime(value),
                },
                {
                  header: 'Asistencia',
                  accessor: 'attended',
                  render: (value, row) => (
                    <Badge variant={value ? 'success' : 'neutral'}>
                      {value ? 'AsistiÃ³' : 'Pendiente'}
                    </Badge>
                  ),
                },
                {
                  header: '',
                  accessor: 'id',
                  render: (_, row) => (
                    <div className="flex items-center justify-end gap-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        disabled={row.status !== 'reserved' && row.status !== 'attended'}
                        onClick={() => toggleAttendance(row, !row.attended)}
                      >
                        {row.attended ? 'Quitar asistencia' : 'Marcar asistencia'}
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        icon={XCircle}
                        disabled={row.status !== 'reserved' && row.status !== 'waitlist'}
                        onClick={() => cancelReservation(row)}
                      >
                        Cancelar
                      </Button>
                    </div>
                  ),
                },
              ]}
              data={reservations}
              loading={reservationsLoading}
              emptyText="Esta sesiÃ³n no tiene reservas"
              emptyIcon={Users}
            />
          </div>
        )}
      </Modal>

      <ConfirmDialog
        isOpen={!!confirm}
        onClose={() => setConfirm(null)}
        onConfirm={runConfirm}
        loading={confirmLoading}
        title={confirm?.title || 'Confirmar'}
        message={confirm?.message || 'Â¿Deseas continuar?'}
        confirmLabel="Continuar"
        variant={confirm?.variant === 'danger' ? 'danger' : 'primary'}
      />
    </div>
  );
}

