import { useEffect, useState } from 'react';
import api from '../services/api';
import CrudPage from '../components/CrudPage';

export default function Staff() {
  const [users, setUsers] = useState([]);

  useEffect(() => {
    let active = true;
    api.get('/staff/users')
      .then((response) => {
        const list = response.data?.data || response.data?.items || [];
        if (active) setUsers(Array.isArray(list) ? list : []);
      })
      .catch(() => {
        if (active) setUsers([]);
      });
    return () => {
      active = false;
    };
  }, []);

  const userField = users.length > 0
    ? {
        name: 'userId',
        label: 'Usuario del sistema',
        type: 'select',
        required: true,
        options: users.map((user) => ({
          value: user.id,
          label: `${user.fullName || user.username} — ${user.username} (${user.role})`,
        })),
      }
    : {
        name: 'userId',
        label: 'ID de usuario',
        type: 'number',
        required: true,
        min: 1,
        help: 'ID del usuario existente en gym_users.',
      };

  return (
    <CrudPage
      title="Personal"
      subtitle="Empleados del gimnasio y sus datos de acceso"
      endpoint="/staff"
      createLabel="Nuevo empleado"
      emptyText="No hay empleados registrados"
      searchParam="query"
      searchPlaceholder="Buscar por nombre o email"
      columns={[
        { key: 'employeeName', label: 'Nombre' },
        { key: 'username', label: 'Usuario' },
        { key: 'email', label: 'Email' },
        { key: 'role', label: 'Rol' },
        { key: 'position', label: 'Puesto' },
      ]}
      fields={[
        userField,
        { name: 'employeeName', label: 'Nombre completo', required: true },
        { name: 'position', label: 'Puesto' },
        { name: 'department', label: 'Departamento' },
      ]}
      editFields={[
        { name: 'employeeName', label: 'Nombre completo', required: true },
        { name: 'position', label: 'Puesto' },
        { name: 'department', label: 'Departamento' },
      ]}
    />
  );
}
