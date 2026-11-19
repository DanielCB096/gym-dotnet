import { useEffect, useState } from 'react';
import { CalendarDays } from 'lucide-react';
import api from '../services/api';
import CrudPage from './CrudPage';
import Badge from './ui/Badge';
import { formatCurrency, formatDate } from '../utils/format';

const today = () => new Date().toISOString().slice(0, 10);

const addDays = (days) => {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return date.toISOString().slice(0, 10);
};

export default function FisioPackagesPanel({ onViewAppointments }) {
  const [members, setMembers] = useState([]);

  useEffect(() => {
    let active = true;

    api.get('/members', { params: { limit: 500 } })
      .then((response) => {
        if (active) setMembers(response.data?.data || []);
      })
      .catch(() => {
        if (active) setMembers([]);
      });

    return () => { active = false; };
  }, []);

  const memberOptions = members
    .filter((member) => member.status === 'active')
    .map((member) => ({
      value: member.id,
      label: `${member.firstName} ${member.lastName}`,
    }));

  const baseFields = [
    { name: 'name', label: 'Nombre del paquete', required: true, placeholder: 'Ej. Rehabilitación rodilla' },
    {
      name: 'memberId',
      label: 'Socio',
      type: 'select',
      options: memberOptions,
      placeholder: 'Paquete general (sin socio)',
      help: 'Un paquete general se asigna al socio en su primera cita.',
    },
    { name: 'sessions', label: 'Número de sesiones', type: 'number', min: 1, defaultValue: 10 },
    { name: 'price', label: 'Precio', type: 'number', min: 0, step: '0.01', defaultValue: 0 },
    { name: 'startDate', label: 'Fecha de inicio', type: 'date', required: true, defaultValue: today() },
    { name: 'endDate', label: 'Fecha de fin', type: 'date', required: true, defaultValue: addDays(30) },
  ];

  return (
    <CrudPage
      hideHeader
      title="Paquete"
      subtitle="Paquetes de fisioterapia"
      endpoint="/fisio/packages"
      extraParams={{ includeInactive: true }}
      createLabel="Nuevo paquete"
      emptyText="No hay paquetes de fisioterapia registrados"
      columns={[
        {
          key: 'name',
          label: 'Paquete',
          render: (row) => (
            <div>
              <p className="font-medium text-slate-800">{row.name}</p>
              <p className="text-xs text-slate-400">{row.memberName || 'General'}</p>
            </div>
          ),
        },
        {
          key: 'sessions',
          label: 'Sesiones',
          render: (row) => {
            const total = row.sessionsTotal ?? row.sessions ?? 0;
            const used = row.sessionsUsed ?? 0;
            const ratio = total > 0 ? Math.min(100, (used / total) * 100) : 0;
            return (
              <div className="min-w-[110px]">
                <div className="flex items-center justify-between text-xs text-slate-500">
                  <span>{used}/{total}</span>
                  <span>{row.sessionsRemaining ?? Math.max(0, total - used)} restantes</span>
                </div>
                <div className="mt-1 h-1.5 w-full overflow-hidden rounded-full bg-slate-100">
                  <div className="h-full bg-brand-500" style={{ width: `${ratio}%` }} />
                </div>
              </div>
            );
          },
        },
        { key: 'price', label: 'Precio', render: (row) => formatCurrency(row.price) },
        {
          key: 'startDate',
          label: 'Vigencia',
          render: (row) => (
            <span className="text-xs text-slate-500">
              {formatDate(row.startDate)} → {formatDate(row.endDate)}
            </span>
          ),
        },
        {
          key: 'isActive',
          label: 'Estado',
          render: (row) => {
            const remaining = row.sessionsRemaining ?? Math.max(0, (row.sessionsTotal ?? 0) - (row.sessionsUsed ?? 0));
            if (!row.isActive) return <Badge variant="neutral" dot>Inactivo</Badge>;
            if (remaining === 0) return <Badge variant="warning" dot>Sin sesiones</Badge>;
            return <Badge variant="success" dot>Activo</Badge>;
          },
        },
      ]}
      fields={baseFields}
      createFields={baseFields}
      editFields={[
        ...baseFields,
        {
          name: 'isActive',
          label: 'Estado',
          type: 'select',
          options: [
            { value: 'true', label: 'Activo' },
            { value: 'false', label: 'Inactivo' },
          ],
        },
      ]}
      mapItemToForm={(item, initial) => ({
        ...initial,
        isActive: item.isActive ? 'true' : 'false',
      })}
      actions={[
        {
          label: 'Ver citas',
          icon: CalendarDays,
          onClick: (row) => onViewAppointments(row.memberId || ''),
        },
      ]}
    />
  );
}
