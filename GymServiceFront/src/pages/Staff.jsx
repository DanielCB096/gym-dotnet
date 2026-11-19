import { useCallback, useEffect, useState } from 'react';
import api from '../services/api';
import CrudPage from '../components/CrudPage';
import StaffUsersPanel from '../components/StaffUsersPanel';
import PageHeader from '../components/ui/PageHeader';
import Badge from '../components/ui/Badge';
import { formatDate } from '../utils/format';

const today = () => new Date().toISOString().slice(0, 10);

const roleVariant = {
  OWNER: 'danger',
  ADMIN: 'warning',
  STAFF: 'info',
  CASHIER: 'neutral',
};

export default function Staff() {
  const [tab, setTab] = useState('employees');
  const [users, setUsers] = useState([]);
  const [availableUsers, setAvailableUsers] = useState([]);
  const [usersLoading, setUsersLoading] = useState(true);

  const loadCatalogs = useCallback(async () => {
    setUsersLoading(true);
    const [allResponse, availableResponse] = await Promise.allSettled([
      api.get('/staff/users/all'),
      api.get('/staff/users'),
    ]);

    if (allResponse.status === 'fulfilled') {
      setUsers(allResponse.value.data?.data || []);
    } else {
      setUsers([]);
    }

    if (availableResponse.status === 'fulfilled') {
      setAvailableUsers(availableResponse.value.data?.data || []);
    } else {
      setAvailableUsers([]);
    }

    setUsersLoading(false);
  }, []);

  useEffect(() => {
    loadCatalogs();
  }, [loadCatalogs]);

  const userOptions = availableUsers.map((user) => ({
    value: user.id,
    label: `${user.fullName || user.username} — ${user.username} (${user.role})`,
  }));

  const employeeFields = [
    {
      name: 'userId',
      label: 'Usuario del sistema',
      type: 'select',
      required: true,
      options: userOptions,
      placeholder: availableUsers.length > 0 ? 'Selecciona un usuario' : 'Sin usuarios disponibles',
      help: availableUsers.length > 0
        ? 'Solo se muestran usuarios activos sin empleado vinculado.'
        : 'Crea primero un usuario del sistema en la pestaña Usuarios.',
    },
    { name: 'employeeName', label: 'Nombre completo', required: true },
    { name: 'position', label: 'Puesto' },
    { name: 'department', label: 'Departamento' },
    { name: 'hireDate', label: 'Fecha de alta', type: 'date', defaultValue: today() },
  ];

  return (
    <div className="animate-fade-in">
      <PageHeader
        title="Personal"
        subtitle="Empleados, usuarios del sistema y sus datos de acceso"
      />

      <div className="mb-6 inline-flex rounded-lg border border-slate-200 bg-white p-1 shadow-sm">
        <button
          type="button"
          onClick={() => setTab('employees')}
          className={`rounded-md px-4 py-2 text-sm font-medium transition-colors ${
            tab === 'employees' ? 'bg-brand-600 text-white' : 'text-slate-600 hover:bg-slate-50'
          }`}
        >
          Empleados
        </button>
        <button
          type="button"
          onClick={() => setTab('users')}
          className={`rounded-md px-4 py-2 text-sm font-medium transition-colors ${
            tab === 'users' ? 'bg-brand-600 text-white' : 'text-slate-600 hover:bg-slate-50'
          }`}
        >
          Usuarios
        </button>
      </div>

      {tab === 'employees' ? (
        <CrudPage
          hideHeader
          title="Empleado"
          subtitle="Empleados del gimnasio"
          endpoint="/staff"
          extraParams={{ includeInactive: true }}
          createLabel="Nuevo empleado"
          emptyText="No hay empleados registrados"
          searchParam="query"
          searchPlaceholder="Buscar por nombre, usuario o email"
          afterCreate={loadCatalogs}
          columns={[
            {
              key: 'employeeName',
              label: 'Empleado',
              render: (row) => (
                <div>
                  <p className="font-medium text-slate-800">{row.employeeName}</p>
                  <p className="text-xs text-slate-400">{row.username || '—'}</p>
                </div>
              ),
            },
            { key: 'email', label: 'Email', render: (row) => row.email || '—' },
            {
              key: 'role',
              label: 'Rol',
              render: (row) => (
                <Badge variant={roleVariant[row.role] || 'neutral'}>{row.role || '—'}</Badge>
              ),
            },
            { key: 'position', label: 'Puesto', render: (row) => row.position || '—' },
            { key: 'department', label: 'Departamento', render: (row) => row.department || '—' },
            {
              key: 'hireDate',
              label: 'Alta',
              render: (row) => (row.hireDate ? formatDate(row.hireDate) : '—'),
            },
            {
              key: 'isActive',
              label: 'Estado',
              render: (row) => (
                <Badge variant={row.isActive ? 'success' : 'neutral'} dot>
                  {row.isActive ? 'Activo' : 'Inactivo'}
                </Badge>
              ),
            },
          ]}
          fields={employeeFields}
          createFields={employeeFields}
          editFields={[
            { name: 'employeeName', label: 'Nombre completo', required: true },
            { name: 'position', label: 'Puesto' },
            { name: 'department', label: 'Departamento' },
            { name: 'hireDate', label: 'Fecha de alta', type: 'date' },
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
          toolbar={() => (
            <div className="flex items-center gap-2">
              {availableUsers.length === 0 && (
                <span className="text-xs text-warning-600">
                  No hay usuarios disponibles para vincular.
                </span>
              )}
              <button
                type="button"
                onClick={() => setTab('users')}
                className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
              >
                Gestionar usuarios
              </button>
            </div>
          )}
        />
      ) : (
        <StaffUsersPanel
          users={users}
          loading={usersLoading}
          onReload={loadCatalogs}
        />
      )}
    </div>
  );
}
