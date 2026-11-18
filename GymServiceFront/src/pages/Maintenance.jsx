import { useState } from 'react';
import { Wrench, AlertCircle } from 'lucide-react';
import api from '../services/api';
import CrudPage from '../components/CrudPage';
import Badge from '../components/ui/Badge';
import Modal from '../components/ui/Modal';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Select from '../components/ui/Select';
import { humanize } from '../utils/format';

const statusVariants = {
  operational: 'success',
  maintenance: 'warning',
  out_of_service: 'danger',
};

export default function Maintenance() {
  const [equipment, setEquipment] = useState(null);
  const [form, setForm] = useState({ description: '', priority: 'normal', assignedTo: '' });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const openTicket = (item) => {
    setEquipment(item);
    setForm({ description: '', priority: 'normal', assignedTo: '' });
    setError('');
    setSuccess('');
  };

  const closeModal = () => {
    if (saving) return;
    setEquipment(null);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');
    try {
      const response = await api.post('/maintenance/tickets', {
        equipmentId: equipment.id,
        description: form.description,
        priority: form.priority,
        assignedTo: form.assignedTo ? Number(form.assignedTo) : null,
      });
      setSuccess(response.data?.message || 'Ticket creado correctamente.');
      setForm({ description: '', priority: 'normal', assignedTo: '' });
    } catch (err) {
      setError(err.response?.data?.message || 'No se pudo crear el ticket.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <CrudPage
        title="Mantenimiento"
        subtitle="Equipos del gimnasio y reportes de fallas"
        endpoint="/maintenance/equipment"
        emptyText="No hay equipos registrados"
        canCreate={false}
        canEdit={false}
        canDelete={false}
        columns={[
          { key: 'name', label: 'Equipo' },
          { key: 'category', label: 'Categoría' },
          {
            key: 'status',
            label: 'Estado',
            render: (row) => (
              <Badge variant={statusVariants[row.status] || 'neutral'}>{humanize(row.status)}</Badge>
            ),
          },
          { key: 'location', label: 'Ubicación' },
        ]}
        actions={[
          {
            label: 'Reportar falla',
            icon: Wrench,
            onClick: (item) => openTicket(item),
          },
        ]}
      />

      <Modal
        isOpen={!!equipment}
        onClose={closeModal}
        title={`Reportar falla · ${equipment?.name || ''}`}
        size="md"
      >
        <form onSubmit={handleSubmit} className="space-y-4">
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
            id="priority"
            label="Prioridad"
            value={form.priority}
            onChange={(event) => setForm({ ...form, priority: event.target.value })}
            options={[
              { value: 'low', label: 'Baja' },
              { value: 'normal', label: 'Normal' },
              { value: 'high', label: 'Alta' },
              { value: 'urgent', label: 'Urgente' },
            ]}
          />
          <Input
            id="assignedTo"
            label="ID del técnico (opcional)"
            type="number"
            min="1"
            value={form.assignedTo}
            onChange={(event) => setForm({ ...form, assignedTo: event.target.value })}
            placeholder="ID del usuario asignado"
          />

          {error && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}
          {success && (
            <div className="rounded-lg border border-success-200 bg-success-50 px-3 py-2 text-sm text-success-700">
              {success}
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={closeModal} disabled={saving}>
              Cerrar
            </Button>
            <Button type="submit" loading={saving}>
              Crear ticket
            </Button>
          </div>
        </form>
      </Modal>
    </>
  );
}
