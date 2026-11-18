import { useState } from 'react';
import { Copy, Power, CheckCircle2 } from 'lucide-react';
import api from '../services/api';
import CrudPage from '../components/CrudPage';
import Badge from '../components/ui/Badge';
import Modal from '../components/ui/Modal';
import Button from '../components/ui/Button';
import { formatDateTime } from '../utils/format';

export default function Kiosk() {
  const [token, setToken] = useState('');
  const [copied, setCopied] = useState(false);

  const closeToken = () => {
    setToken('');
    setCopied(false);
  };

  const copyToken = async () => {
    try {
      await navigator.clipboard.writeText(token);
      setCopied(true);
    } catch {
      setCopied(false);
    }
  };

  return (
    <>
      <CrudPage
        title="Kioscos"
        subtitle="Dispositivos de autoservicio registrados"
        endpoint="/kiosk/devices"
        createLabel="Registrar kiosco"
        emptyText="No hay dispositivos registrados"
        canEdit={false}
        canDelete={false}
        createPath="/kiosk/device/register"
        createPayload={(form) => ({ deviceName: form.deviceName })}
        afterCreate={(data, { notify }) => {
          if (data?.token) {
            setToken(data.token);
          } else {
            notify('success', data?.message || 'Dispositivo registrado.');
          }
        }}
        columns={[
          { key: 'deviceName', label: 'Dispositivo' },
          {
            key: 'isActive',
            label: 'Estado',
            render: (row) => (
              <Badge variant={row.isActive ? 'success' : 'neutral'} dot>
                {row.isActive ? 'Activo' : 'Inactivo'}
              </Badge>
            ),
          },
          { key: 'createdAt', label: 'Registrado', render: (row) => formatDateTime(row.createdAt) },
        ]}
        fields={[
          {
            name: 'deviceName',
            label: 'Nombre del dispositivo',
            required: true,
            placeholder: 'Ej. Kiosco recepción',
            full: true,
          },
        ]}
        actions={[
          {
            label: 'Desactivar',
            icon: Power,
            variant: 'danger',
            confirm: 'El dispositivo dejará de aceptar accesos. ¿Deseas desactivarlo?',
            confirmLabel: 'Desactivar',
            hidden: (item) => !item.isActive,
            onClick: async (item, { reload, notify }) => {
              await api.put(`/kiosk/devices/${item.id}/deactivate`);
              notify('success', 'Dispositivo desactivado correctamente.');
              reload();
            },
          },
        ]}
      />

      <Modal isOpen={!!token} onClose={closeToken} title="Token del dispositivo" size="md">
        <div className="space-y-4">
          <div className="flex items-start gap-2.5 rounded-lg border border-warning-200 bg-warning-50 px-3 py-2 text-sm text-warning-700">
            <CheckCircle2 className="w-4 h-4 mt-0.5 flex-shrink-0" />
            <p>Guarda este token ahora. No se volverá a mostrar.</p>
          </div>
          <code className="block break-all rounded-lg bg-slate-900 px-4 py-3 text-xs text-slate-100">
            {token}
          </code>
          <div className="flex justify-end gap-2">
            <Button variant="ghost" onClick={closeToken}>
              Cerrar
            </Button>
            <Button icon={Copy} onClick={copyToken}>
              {copied ? 'Copiado' : 'Copiar token'}
            </Button>
          </div>
        </div>
      </Modal>
    </>
  );
}
