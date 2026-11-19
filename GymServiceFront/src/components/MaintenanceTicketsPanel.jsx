import { useCallback, useEffect, useState } from 'react';
import {
  AlertCircle,
  CheckCircle2,
  Play,
  Plus,
  Trash2,
  Wrench,
  XCircle,
} from 'lucide-react';
import api from '../services/api';
import Modal from './ui/Modal';
import Button from './ui/Button';
import Select from './ui/Select';
import Table from './ui/Table';
import Badge from './ui/Badge';
import ConfirmDialog from './ui/ConfirmDialog';
import { formatDateTime } from '../utils/format';

const statusVariant = {
  open: 'warning',
  in_progress: 'info',
  resolved: 'success',
  cancelled: 'neutral',
};

const statusLabel = {
  open: 'Abierto',
  in_progress: 'En progreso',
  resolved: 'Resuelto',
  cancelled: 'Cancelado',
};

const priorityVariant = {
  low: 'neutral',
  normal: 'info',
  high: 'warning',
  urgent: 'danger',
};

const priorityLabel = {
  low: 'Baja',
  normal: 'Normal',
  high: 'Alta',
  urgent: 'Urgente',
};

const statusOptions = [
  { value: 'open', label: 'Abierto' },
  { value: 'in_progress', label: 'En progreso' },
  { value: 'resolved', label: 'Resuelto' },
  { value: 'cancelled', label: 'Cancelado' },
];

const priorityOptions = [
  { value: 'low', label: 'Baja' },
  { value: 'normal', label: 'Normal' },
  { value: 'high', label: 'Alta' },
  { value: 'urgent', label: 'Urgente' },
];

const errorMessage = (err, fallback) => err?.response?.data?.message || fallback;

export default function MaintenanceTicketsPanel({ initialEquipmentId = '' }) {
  const [equipment, setEquipment] = useState([]);
  const [tickets, setTickets] = useState([]);
  const [pagination, setPagination] = useState(null);
  const [filterEquipment, setFilterEquipment] = useState(initialEquipmentId ? String(initialEquipmentId) : '');
  const [filterStatus, setFilterStatus] = useState('');
  const [filterPriority, setFilterPriority] = useState('');
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState(null);

  const [modalOpen, setModalOpen] = useState(false);
  const [form, setForm] = useState({ equipmentId: '', description: '', priority: 'normal' });
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);

  const [confirm, setConfirm] = useState(null);
  const [confirmLoading, setConfirmLoading] = useState(false);

  const notify = (type, message) => {
    setNotice({ type, message });
    window.clearTimeout(notify.timer);
    notify.timer = window.setTimeout(() => setNotice(null), 4500);
  };

  const loadEquipment = useCallback(async () => {
    try {
      const response = await api.get('/maintenance/equipment');
      setEquipment(response.data?.data || []);
    } catch {
      setEquipment([]);
    }
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const params = { page, limit: 50 };
      if (filterEquipment) params.equipmentId = Number(filterEquipment);
      if (filterStatus) params.status = filterStatus;
      if (filterPriority) params.priority = filterPriority;

      const response = await api.get('/maintenance/tickets', { params });
      setTickets(response.data?.data || []);
      setPagination(response.data?.pagination || null);
    } catch (err) {
      setTickets([]);
      setPagination(null);
      setError(errorMessage(err, 'No se pudieron cargar los tickets.'));
    } finally {
      setLoading(false);
    }
  }, [page, filterEquipment, filterStatus, filterPriority]);

  useEffect(() => { loadEquipment(); }, [loadEquipment]);
  useEffect(() => { load(); }, [load]);

  const openCreate = (equipmentId = '') => {
    setForm({
      equipmentId: equipmentId ? String(equipmentId) : (filterEquipment || ''),
      description: '',
      priority: 'normal',
    });
    setFormError('');
    setModalOpen(true);
  };

  const create = async (event) => {
    event.preventDefault();
    setSaving(true);
    setFormError('');

    try {
      const response = await api.post('/maintenance/tickets', {
        equipmentId: Number(form.equipmentId) || 0,
        description: form.description,
        priority: form.priority,
      });

      notify('success', response.data?.message || 'Ticket creado correctamente.');
      setModalOpen(false);
      await Promise.all([load(), loadEquipment()]);
    } catch (err) {
      setFormError(errorMessage(err, 'No se pudo crear el ticket.'));
    } finally {
      setSaving(false);
    }
  };

  const changeStatus = (row, status, title, message) => {
    setConfirm({
      title,
      message,
      variant: status === 'resolved' ? 'primary' : 'danger',
      action: async () => {
        const response = await api.put(`/maintenance/tickets/${row.id}/status`, { status });
        notify('success', response.data?.message || 'Ticket actualizado.');
        await Promise.all([load(), loadEquipment()]);
      },
    });
  };

  const remove = (row) => {
    setConfirm({
      title: 'Eliminar ticket',
      message: `Se eliminará el ticket ${row.ticketNumber}. ¿Continuar?`,
      variant: 'danger',
      action: async () => {
        await api.delete(`/maintenance/tickets/${row.id}`);
        notify('success', 'Ticket eliminado.');
        await Promise.all([load(), loadEquipment()]);
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
      header: 'Ticket',
      accessor: 'ticketNumber',
      render: (value, row) => (
        <div>
          <p className="font-medium text-slate-800">{value}</p>
          <p className="text-xs text-slate-400">{formatDateTime(row.createdAt)}</p>
        </div>
      ),
    },
    {
      header: 'Equipo',
      accessor: 'equipmentName',
      render: (value, row) => (
        <div>
          <p className="font-medium text-slate-800">{value}</p>
          <p className="text-xs text-slate-400">{row.description}</p>
        </div>
      ),
    },
    {
      header: 'Prioridad',
      accessor: 'priority',
      render: (value) => (
        <Badge variant={priorityVariant[value] || 'neutral'}>
          {priorityLabel[value] || value}
        </Badge>
      ),
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
    {
      header: 'Resolución',
      accessor: 'resolvedAt',
      render: (value, row) => (
        value
          ? (
            <span className="text-xs text-slate-500">
              {formatDateTime(value)}{row.resolvedByName ? ` · ${row.resolvedByName}` : ''}
            </span>
          )
          : <span className="text-slate-400">—</span>
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
            icon={Play}
            disabled={row.status !== 'open'}
            onClick={() => changeStatus(row, 'in_progress', 'Iniciar ticket', 'El ticket pasará a en progreso. ¿Continuar?')}
          >
            Iniciar
          </Button>
          <Button
            variant="ghost"
            size="sm"
            icon={CheckCircle2}
            disabled={row.status !== 'open' && row.status !== 'in_progress'}
            onClick={() => changeStatus(row, 'resolved', 'Resolver ticket', 'El equipo volverá a operativo si no tiene otros tickets abiertos. ¿Continuar?')}
          >
            Resolver
          </Button>
          <Button
            variant="ghost"
            size="sm"
            icon={XCircle}
            disabled={row.status !== 'open' && row.status !== 'in_progress'}
            onClick={() => changeStatus(row, 'cancelled', 'Cancelar ticket', 'El ticket quedará cancelado. ¿Continuar?')}
          >
            Cancelar
          </Button>
          <Button
            variant="ghost"
            size="sm"
            icon={Trash2}
            disabled={row.status === 'resolved'}
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
          label="Equipo"
          value={filterEquipment}
          onChange={(event) => { setFilterEquipment(event.target.value); setPage(1); }}
          options={equipment.map((item) => ({ value: item.id, label: item.name }))}
          placeholder="Todos"
        />
        <Select
          label="Estado"
          value={filterStatus}
          onChange={(event) => { setFilterStatus(event.target.value); setPage(1); }}
          options={statusOptions}
          placeholder="Todos"
        />
        <Select
          label="Prioridad"
          value={filterPriority}
          onChange={(event) => { setFilterPriority(event.target.value); setPage(1); }}
          options={priorityOptions}
          placeholder="Todas"
        />
        <Button icon={Plus} onClick={() => openCreate()} className="lg:mb-0.5">
          Nuevo ticket
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
        data={tickets}
        loading={loading}
        emptyText="No hay tickets de mantenimiento"
        emptyIcon={Wrench}
      />

      {pagination && pagination.totalCount > 0 && (
        <div className="flex items-center justify-between text-sm text-slate-500">
          <span>
            {pagination.totalCount} ticket(s) · página {pagination.page} de {totalPages}
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
        isOpen={modalOpen}
        onClose={() => !saving && setModalOpen(false)}
        title="Nuevo ticket de mantenimiento"
        size="md"
      >
        <form onSubmit={create} className="space-y-4">
          <Select
            label="Equipo"
            value={form.equipmentId}
            onChange={(event) => setForm({ ...form, equipmentId: event.target.value })}
            options={equipment.map((item) => ({
              value: item.id,
              label: `${item.name}${item.location ? ` · ${item.location}` : ''}`,
            }))}
            placeholder="Selecciona un equipo"
            required
          />
          <div>
            <label htmlFor="ticketDescription" className="block text-sm font-medium text-slate-700 mb-1.5">
              Descripción del problema
            </label>
            <textarea
              id="ticketDescription"
              rows={4}
              required
              value={form.description}
              onChange={(event) => setForm({ ...form, description: event.target.value })}
              placeholder="Describe la falla del equipo"
              className="block w-full rounded-lg border border-slate-300 bg-white text-sm text-slate-800 placeholder-slate-400 px-3.5 py-2.5 focus:outline-none focus:ring-2 focus:ring-brand-500/50 focus:border-brand-500"
            />
          </div>
          <Select
            label="Prioridad"
            value={form.priority}
            onChange={(event) => setForm({ ...form, priority: event.target.value })}
            options={priorityOptions}
          />

          {formError && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{formError}</span>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={() => setModalOpen(false)} disabled={saving}>
              Cancelar
            </Button>
            <Button type="submit" loading={saving} disabled={!form.equipmentId || !form.description.trim()}>
              Crear ticket
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
