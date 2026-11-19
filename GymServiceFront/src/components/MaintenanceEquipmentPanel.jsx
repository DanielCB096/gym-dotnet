import { Wrench } from 'lucide-react';
import CrudPage from './CrudPage';
import Badge from './ui/Badge';
import { humanize } from '../utils/format';

const statusVariants = {
  operational: 'success',
  maintenance: 'warning',
  out_of_service: 'danger',
};

const statusOptions = [
  { value: 'operational', label: 'Operativo' },
  { value: 'maintenance', label: 'En mantenimiento' },
  { value: 'out_of_service', label: 'Fuera de servicio' },
];

export default function MaintenanceEquipmentPanel({ onReportTicket }) {
  return (
    <CrudPage
      hideHeader
      title="Equipo"
      subtitle="Equipos del gimnasio"
      endpoint="/maintenance/equipment"
      createLabel="Nuevo equipo"
      emptyText="No hay equipos registrados"
      columns={[
        {
          key: 'name',
          label: 'Equipo',
          render: (row) => (
            <div>
              <p className="font-medium text-slate-800">{row.name}</p>
              <p className="text-xs text-slate-400">{row.location || 'Sin ubicación'}</p>
            </div>
          ),
        },
        { key: 'category', label: 'Categoría', render: (row) => row.category || '—' },
        {
          key: 'status',
          label: 'Estado',
          render: (row) => (
            <Badge variant={statusVariants[row.status] || 'neutral'} dot>
              {humanize(row.status)}
            </Badge>
          ),
        },
        {
          key: 'openTickets',
          label: 'Tickets abiertos',
          render: (row) => (
            row.openTickets > 0
              ? <Badge variant="warning">{row.openTickets}</Badge>
              : <span className="text-slate-400">0</span>
          ),
        },
        {
          key: 'purchaseDate',
          label: 'Garantía',
          render: (row) => (
            <span className="text-xs text-slate-500">
              {row.warrantyEnd ? `Hasta ${String(row.warrantyEnd).slice(0, 10)}` : '—'}
            </span>
          ),
        },
      ]}
      fields={[
        { name: 'name', label: 'Nombre del equipo', required: true, placeholder: 'Ej. Caminadora 3' },
        { name: 'category', label: 'Categoría', placeholder: 'Ej. Cardio' },
        { name: 'location', label: 'Ubicación', placeholder: 'Ej. Sala 1' },
        { name: 'status', label: 'Estado', type: 'select', options: statusOptions, defaultValue: 'operational' },
        { name: 'purchaseDate', label: 'Fecha de compra', type: 'date' },
        { name: 'warrantyEnd', label: 'Fin de garantía', type: 'date' },
      ]}
      actions={[
        {
          label: 'Reportar falla',
          icon: Wrench,
          onClick: (row) => onReportTicket(row.id),
        },
      ]}
    />
  );
}
