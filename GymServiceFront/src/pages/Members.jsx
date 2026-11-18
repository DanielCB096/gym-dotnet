import { useEffect, useState } from 'react';
import api from '../services/api';
import CrudPage from '../components/CrudPage';
import Badge from '../components/ui/Badge';
import { formatCurrency, formatDate } from '../utils/format';

export default function Members() {
  const [plans, setPlans] = useState([]);
  const [plansError, setPlansError] = useState('');

  useEffect(() => {
    let active = true;
    api.get('/members/plans')
      .then((response) => {
        const list = response.data?.data || response.data?.items || [];
        if (active) setPlans(Array.isArray(list) ? list : []);
      })
      .catch((err) => {
        if (active) {
          setPlans([]);
          setPlansError(err.response?.data?.message || 'No se pudieron cargar los planes de membresía.');
        }
      });
    return () => {
      active = false;
    };
  }, []);

  const planField = plans.length > 0
    ? {
        name: 'membershipType',
        label: 'Plan de membresía',
        type: 'select',
        placeholder: 'Sin membresía',
        help: 'El plan seleccionado se asigna por 30 días desde hoy.',
        options: plans.map((plan) => ({
          value: plan.planName,
          label: `${plan.planName} — ${formatCurrency(plan.price)} · ${plan.durationDays} días`,
        })),
      }
    : {
        name: 'membershipType',
        label: 'Plan (nombre exacto)',
        help: plansError || 'No hay planes activos registrados; el socio se creará sin membresía.',
      };

  return (
    <CrudPage
      title="Miembros"
      subtitle="Socios del gimnasio y su membresía actual"
      endpoint="/members"
      createLabel="Nuevo miembro"
      emptyText="No hay miembros registrados"
      searchParam="query"
      searchPlaceholder="Buscar por nombre o email"
      canEdit={false}
      canDelete={false}
      columns={[
        {
          key: 'firstName',
          label: 'Nombre',
          render: (row) => (
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-full bg-gradient-to-br from-brand-400 to-brand-600 flex items-center justify-center text-xs font-bold text-white flex-shrink-0">
                {(row.firstName || 'U').charAt(0).toUpperCase()}
              </div>
              <span className="text-sm font-medium text-slate-800">
                {`${row.firstName || ''} ${row.lastName || ''}`.trim() || '—'}
              </span>
            </div>
          ),
        },
        { key: 'email', label: 'Email', render: (row) => row.email || '—' },
        { key: 'phone', label: 'Teléfono', render: (row) => row.phone || '—' },
        {
          key: 'membershipType',
          label: 'Plan',
          render: (row) => (
            row.membershipType
              ? <Badge variant="primary">{row.membershipType}</Badge>
              : <span className="text-slate-400">Sin membresía</span>
          ),
        },
        {
          key: 'status',
          label: 'Estado',
          render: (row) => (
            <Badge variant={row.status === 'active' ? 'success' : 'neutral'} dot>
              {row.status === 'active' ? 'Activo' : 'Inactivo'}
            </Badge>
          ),
        },
        { key: 'membershipEnd', label: 'Vence', render: (row) => formatDate(row.membershipEnd) },
      ]}
      fields={[
        { name: 'firstName', label: 'Nombre', required: true, placeholder: 'Juan' },
        { name: 'lastName', label: 'Apellido', required: true, placeholder: 'Pérez' },
        { name: 'email', label: 'Email', type: 'email', placeholder: 'juan@email.com' },
        { name: 'phone', label: 'Teléfono', type: 'tel', placeholder: '555-1234' },
        planField,
      ]}
    />
  );
}
