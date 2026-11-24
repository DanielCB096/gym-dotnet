import { useEffect, useState } from 'react';
import { CreditCard, Upload, XCircle, FileText } from 'lucide-react';
import api from '../services/api';
import CrudPage from '../components/CrudPage';
import ImportDialog from '../components/ImportDialog';
import MembershipModal from '../components/MembershipModal';
import CancelMembershipModal from '../components/CancelMembershipModal';
import MemberProfilePreview from '../components/MemberProfilePreview';
import Badge from '../components/ui/Badge';
import Button from '../components/ui/Button';
import { formatCurrency, formatDate } from '../utils/format';

const membershipStatusInfo = {
  active: { variant: 'success', label: 'Activo' },
  frozen: { variant: 'warning', label: 'Congelada' },
  cancelled: { variant: 'danger', label: 'Cancelada' },
  expired: { variant: 'danger', label: 'Vencida' },
  none: { variant: 'warning', label: 'Sin membresía' },
};

export default function Members() {
  const [plans, setPlans] = useState([]);
  const [plansError, setPlansError] = useState('');
  const [membershipTarget, setMembershipTarget] = useState(null);
  const [cancelTarget, setCancelTarget] = useState(null);
  const [profileTarget, setProfileTarget] = useState(null);
  const [importTarget, setImportTarget] = useState(null);

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
    <>
      <CrudPage
      title="Miembros"
      subtitle="Socios del gimnasio y su membresía actual"
      endpoint="/members"
      createLabel="Nuevo miembro"
      emptyText="No hay miembros registrados"
      searchParam="query"
      searchPlaceholder="Buscar por nombre o email"
      toolbar={({ reload }) => (
        <Button
          variant="outline"
          icon={Upload}
          onClick={() => setImportTarget({ reload })}
          className="w-full sm:w-auto"
        >
          Importar
        </Button>
      )}
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
            <div>
              {row.membershipType
                ? <Badge variant="primary">{row.membershipType}</Badge>
                : <span className="text-slate-400">Sin membresía</span>}
              {Number(row.creditBalance) > 0 && (
                <p className="mt-1 text-xs font-medium text-success-600">
                  Crédito: {formatCurrency(row.creditBalance)}
                </p>
              )}
            </div>
          ),
        },
        {
          key: 'status',
          label: 'Estado',
          render: (row) => {
            if (row.status !== 'active') {
              return <Badge variant="neutral" dot>Inactivo</Badge>;
            }

            const info = membershipStatusInfo[row.membershipStatus] || membershipStatusInfo.none;
            return <Badge variant={info.variant} dot>{info.label}</Badge>;
          },
        },
        { key: 'membershipEnd', label: 'Vence', render: (row) => formatDate(row.membershipEnd) },
      ]}
      canEdit
      fields={[
        { name: 'firstName', label: 'Nombre', required: true, placeholder: 'Juan' },
        { name: 'lastName', label: 'Apellido', required: true, placeholder: 'Pérez' },
        { name: 'email', label: 'Email', type: 'email', placeholder: 'juan@email.com' },
        { name: 'phone', label: 'Teléfono', type: 'tel', placeholder: '555-1234' },
        { name: 'curp', label: 'CURP', placeholder: '18 caracteres', maxLength: 18 },
        { name: 'rfc', label: 'RFC', placeholder: '13 caracteres', maxLength: 13 },
        { name: 'address', label: 'Dirección', full: true, maxLength: 500 },
        { name: 'emergencyContact', label: 'Contacto de emergencia', maxLength: 200 },
        { name: 'emergencyPhone', label: 'Teléfono de emergencia', type: 'tel', maxLength: 20 },
        { name: 'photoUrl', label: 'URL de foto', full: true, maxLength: 500, placeholder: 'https://…' },
        { name: 'notes', label: 'Notas', type: 'textarea', full: true, rows: 3, maxLength: 2000 },
        planField,
      ]}
      editFields={[
        { name: 'firstName', label: 'Nombre', required: true },
        { name: 'lastName', label: 'Apellido', required: true },
        { name: 'email', label: 'Email', type: 'email' },
        { name: 'phone', label: 'Teléfono', type: 'tel' },
        { name: 'curp', label: 'CURP', maxLength: 18 },
        { name: 'rfc', label: 'RFC', maxLength: 13 },
        { name: 'address', label: 'Dirección', full: true, maxLength: 500 },
        { name: 'emergencyContact', label: 'Contacto de emergencia', maxLength: 200 },
        { name: 'emergencyPhone', label: 'Teléfono de emergencia', type: 'tel', maxLength: 20 },
        { name: 'photoUrl', label: 'URL de foto', full: true, maxLength: 500 },
        { name: 'notes', label: 'Notas', type: 'textarea', full: true, rows: 3, maxLength: 2000 },
        {
          name: 'status',
          label: 'Estado de la cuenta',
          type: 'select',
          options: [
            { value: 'active', label: 'Activo' },
            { value: 'inactive', label: 'Inactivo' },
          ],
        },
      ]}
      actions={[
        {
          label: 'Ficha',
          icon: FileText,
          onClick: (row) => setProfileTarget(row),
        },
        {
          label: 'Membresía',
          icon: CreditCard,
          onClick: (row, { reload, notify }) => setMembershipTarget({ member: row, reload, notify }),
        },
        {
          label: 'Cancelar membresía',
          icon: XCircle,
          variant: 'danger',
          hidden: (row) => !row.membershipId || (row.membershipStatus !== 'active' && row.membershipStatus !== 'frozen'),
          onClick: (row, { reload, notify }) => setCancelTarget({ member: row, reload, notify }),
        },
      ]}
    />

      {membershipTarget && (
        <MembershipModal
          key={membershipTarget.member.id}
          target={membershipTarget}
          plans={plans}
          onClose={() => setMembershipTarget(null)}
          onSaved={(message) => {
            membershipTarget.notify('success', message);
            membershipTarget.reload();
          }}
        />
      )}

      {cancelTarget && (
        <CancelMembershipModal
          key={`cancel-${cancelTarget.member.id}`}
          target={cancelTarget}
          onClose={() => setCancelTarget(null)}
          onCancelled={(message) => {
            cancelTarget.notify('success', message);
            cancelTarget.reload();
          }}
        />
      )}

      <MemberProfilePreview member={profileTarget} onClose={() => setProfileTarget(null)} />

      {importTarget && (
        <ImportDialog
          entity="members"
          isOpen
          onClose={() => setImportTarget(null)}
          onImported={() => importTarget.reload()}
        />
      )}
    </>
  );
}
