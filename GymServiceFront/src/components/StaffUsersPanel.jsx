import { useState } from 'react';
import { AlertCircle, CheckCircle2, Plus, ShieldCheck, ShieldOff } from 'lucide-react';
import api from '../services/api';
import { useAuth } from '../context/AuthContext';
import Modal from './ui/Modal';
import Button from './ui/Button';
import Input from './ui/Input';
import Select from './ui/Select';
import Table from './ui/Table';
import Badge from './ui/Badge';

const roleVariant = {
  OWNER: 'danger',
  ADMIN: 'warning',
  STAFF: 'info',
  CASHIER: 'neutral',
};

const roleOptions = [
  { value: 'ADMIN', label: 'Administrador' },
  { value: 'STAFF', label: 'Personal' },
  { value: 'CASHIER', label: 'Cajero' },
];

const errorMessage = (err, fallback) => err?.response?.data?.message || fallback;

export default function StaffUsersPanel({ users, loading, onReload }) {
  const { user: currentUser } = useAuth();
  const [open, setOpen] = useState(false);
  const [form, setForm] = useState({ username: '', fullName: '', email: '', role: 'STAFF', password: '' });
  const [saving, setSaving] = useState(false);
  const [resettingId, setResettingId] = useState(null);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState(null);

  const openModal = () => {
    setForm({ username: '', fullName: '', email: '', role: 'STAFF', password: '' });
    setError('');
    setNotice(null);
    setOpen(true);
  };

  const save = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');

    try {
      const response = await api.post('/staff/users', {
        username: form.username.trim(),
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        role: form.role,
        password: form.password,
      });

      setNotice(response.data?.message || 'Usuario creado correctamente.');
      setOpen(false);
      await onReload();
    } catch (err) {
      setError(errorMessage(err, 'No se pudo crear el usuario.'));
    } finally {
      setSaving(false);
    }
  };

  const resetMfa = async (row) => {
    const confirmed = window.confirm(
      `¿Resetear el MFA de "${row.username}"?\n\nSe borrarán su secreto y códigos de respaldo, y se cerrarán sus sesiones activas.`
    );
    if (!confirmed) return;

    setResettingId(row.id);
    setError('');
    setNotice(null);
    try {
      const response = await api.post('/auth/mfa/reset', { userId: row.id });
      setNotice(response.data?.message || 'MFA reseteado correctamente.');
      await onReload();
    } catch (err) {
      setError(errorMessage(err, 'No se pudo resetear el MFA.'));
    } finally {
      setResettingId(null);
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-slate-500">
          Los usuarios del sistema se vinculan a un empleado para controlar su acceso y rol.
        </p>
        <Button icon={Plus} onClick={openModal}>Nuevo usuario</Button>
      </div>

      {notice && (
        <div className="flex items-start gap-2 rounded-lg border border-success-200 bg-success-50 px-3 py-2 text-sm text-success-700">
          <CheckCircle2 className="h-4 w-4 mt-0.5 flex-shrink-0" />
          <span>{notice}</span>
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
            header: 'Usuario',
            accessor: 'username',
            render: (value, row) => (
              <div>
                <p className="font-medium text-slate-800">{value}</p>
                <p className="text-xs text-slate-400">{row.fullName || 'Sin nombre'}</p>
              </div>
            ),
          },
          { header: 'Email', accessor: 'email', render: (value) => value || '—' },
          {
            header: 'Rol',
            accessor: 'role',
            render: (value) => (
              <Badge variant={roleVariant[value] || 'neutral'}>{value}</Badge>
            ),
          },
          {
            header: 'Empleado',
            accessor: 'employeeName',
            render: (value) => (
              value
                ? <span className="text-slate-700">{value}</span>
                : <span className="text-slate-400">Sin vincular</span>
            ),
          },
          {
            header: 'MFA',
            accessor: 'mfaEnabled',
            render: (value) => (
              <Badge variant={value ? 'success' : 'neutral'} dot>
                {value ? 'Activo' : 'Inactivo'}
              </Badge>
            ),
          },
          {
            header: 'Estado',
            accessor: 'isActive',
            render: (value) => (
              <Badge variant={value ? 'success' : 'neutral'} dot>
                {value ? 'Activo' : 'Inactivo'}
              </Badge>
            ),
          },
          {
            header: 'Acciones',
            accessor: 'id',
            render: (value, row) => {
              if (Number(value) === Number(currentUser?.userId)) {
                return <span className="text-xs text-slate-400">Tu cuenta</span>;
              }
              if (row.role === 'OWNER' && currentUser?.role !== 'OWNER') {
                return <span className="text-xs text-slate-400">—</span>;
              }
              if (!row.mfaEnabled) {
                return <span className="text-xs text-slate-400">—</span>;
              }
              return (
                <Button
                  size="sm"
                  variant="ghost"
                  icon={ShieldOff}
                  loading={resettingId === row.id}
                  onClick={() => resetMfa(row)}
                >
                  Resetear MFA
                </Button>
              );
            },
          },
        ]}
        data={users}
        loading={loading}
        emptyText="No hay usuarios del sistema"
        emptyIcon={ShieldCheck}
      />

      <Modal isOpen={open} onClose={() => !saving && setOpen(false)} title="Nuevo usuario del sistema" size="md">
        <form onSubmit={save} className="space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Input
              label="Usuario"
              value={form.username}
              onChange={(event) => setForm({ ...form, username: event.target.value })}
              placeholder="Ej. recepcion1"
              required
            />
            <Input
              label="Nombre completo"
              value={form.fullName}
              onChange={(event) => setForm({ ...form, fullName: event.target.value })}
              placeholder="Ej. Ana López"
              required
            />
            <Input
              label="Correo"
              type="email"
              value={form.email}
              onChange={(event) => setForm({ ...form, email: event.target.value })}
              placeholder="ana@centrofit.mx"
              required
            />
            <Select
              label="Rol"
              value={form.role}
              onChange={(event) => setForm({ ...form, role: event.target.value })}
              options={roleOptions}
            />
            <Input
              label="Contraseña"
              type="password"
              value={form.password}
              onChange={(event) => setForm({ ...form, password: event.target.value })}
              placeholder="Mínimo 8 caracteres"
              required
              className="sm:col-span-2"
            />
          </div>

          <p className="text-xs text-slate-400">
            Roles disponibles: Administrador (gestión completa), Personal (operación diaria) y Cajero (ventas).
          </p>

          {error && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={() => setOpen(false)} disabled={saving}>
              Cancelar
            </Button>
            <Button
              type="submit"
              loading={saving}
              disabled={!form.username.trim() || !form.fullName.trim() || !form.email.trim() || form.password.length < 8}
            >
              Crear usuario
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
