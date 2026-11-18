import { useState } from 'react';
import { CalendarPlus, AlertCircle } from 'lucide-react';
import api from '../services/api';
import CrudPage from '../components/CrudPage';
import Modal from '../components/ui/Modal';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Select from '../components/ui/Select';
import { formatCurrency, formatDate } from '../utils/format';

const TODAY = new Date().toISOString().slice(0, 10);

function AppointmentButton({ packages, onDone }) {
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({ memberId: '', packageId: '', appointmentDate: '', notes: '' });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const openModal = () => {
    setForm({ memberId: '', packageId: '', appointmentDate: '', notes: '' });
    setError('');
    setOpen(true);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');
    try {
      const response = await api.post('/fisio/appointments', {
        memberId: Number(form.memberId) || 0,
        packageId: Number(form.packageId) || 0,
        appointmentDate: form.appointmentDate,
        notes: form.notes,
      });
      onDone(response.data?.message || 'Cita agendada correctamente.');
      setOpen(false);
    } catch (err) {
      setError(err.response?.data?.message || 'No se pudo agendar la cita.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <Button variant="outline" icon={CalendarPlus} onClick={openModal} disabled={packages.length === 0}>
        Agendar cita
      </Button>
      <Modal isOpen={open} onClose={() => !saving && setOpen(false)} title="Agendar cita de fisioterapia" size="md">
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            id="memberId"
            label="ID del socio"
            type="number"
            min="1"
            value={form.memberId}
            onChange={(event) => setForm({ ...form, memberId: event.target.value })}
            required
          />
          <Select
            id="packageId"
            label="Paquete"
            value={form.packageId}
            onChange={(event) => setForm({ ...form, packageId: event.target.value })}
            placeholder="Selecciona un paquete"
            options={packages.map((item) => ({
              value: item.id,
              label: `${item.name} (${item.sessions} sesiones)`,
            }))}
          />
          <Input
            id="appointmentDate"
            label="Fecha y hora"
            type="datetime-local"
            value={form.appointmentDate}
            onChange={(event) => setForm({ ...form, appointmentDate: event.target.value })}
            required
          />
          <Input
            id="notes"
            label="Notas"
            value={form.notes}
            onChange={(event) => setForm({ ...form, notes: event.target.value })}
            placeholder="Motivo de la cita"
          />
          {error && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={() => setOpen(false)} disabled={saving}>
              Cancelar
            </Button>
            <Button type="submit" loading={saving}>
              Agendar
            </Button>
          </div>
        </form>
      </Modal>
    </>
  );
}

export default function Fisio() {
  return (
    <CrudPage
      title="Fisioterapia"
      subtitle="Paquetes de sesiones y citas"
      endpoint="/fisio/packages"
      createLabel="Nuevo paquete"
      emptyText="No hay paquetes de fisioterapia registrados"
      canEdit={false}
      columns={[
        { key: 'name', label: 'Paquete' },
        { key: 'sessions', label: 'Sesiones' },
        { key: 'price', label: 'Precio', render: (row) => formatCurrency(row.price) },
        { key: 'startDate', label: 'Inicio', render: (row) => formatDate(row.startDate) },
      ]}
      fields={[
        { name: 'name', label: 'Nombre del paquete', required: true },
        { name: 'sessions', label: 'Número de sesiones', type: 'number', min: 1, defaultValue: 10 },
        { name: 'price', label: 'Precio', type: 'number', min: 0, step: '0.01', defaultValue: 0 },
        { name: 'startDate', label: 'Fecha de inicio', type: 'date', required: true, defaultValue: TODAY },
      ]}
      toolbar={({ items, reload, notify }) => (
        <AppointmentButton
          packages={items}
          onDone={(message) => {
            notify('success', message);
            reload();
          }}
        />
      )}
    />
  );
}
