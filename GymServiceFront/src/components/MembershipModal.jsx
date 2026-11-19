import { useEffect, useMemo, useState } from 'react';
import { AlertCircle, CalendarCheck, Printer } from 'lucide-react';
import api from '../services/api';
import Modal from './ui/Modal';
import Button from './ui/Button';
import Input from './ui/Input';
import Select from './ui/Select';
import Badge from './ui/Badge';
import ReceiptPreview from './ReceiptPreview';
import { formatCurrency, formatDate, formatDateTime } from '../utils/format';

const todayInput = () => new Date().toISOString().slice(0, 10);

const membershipLabel = {
  active: { variant: 'success', label: 'Activa' },
  frozen: { variant: 'warning', label: 'Congelada' },
  cancelled: { variant: 'danger', label: 'Cancelada' },
  expired: { variant: 'danger', label: 'Vencida' },
  none: { variant: 'neutral', label: 'Sin membresía' },
};

const paymentOptions = [
  { value: 'cash', label: 'Efectivo' },
  { value: 'card', label: 'Tarjeta (declarada)' },
  { value: 'transfer', label: 'Transferencia' },
  { value: 'none', label: 'Sin cobro (cortesía)' },
];

const paymentMethodLabel = {
  cash: 'Efectivo',
  card: 'Tarjeta',
  transfer: 'Transferencia',
};

const errorMessage = (err, fallback) => err?.response?.data?.message || fallback;

export default function MembershipModal({ target, plans, onClose, onSaved }) {
  const member = target?.member;
  const [planId, setPlanId] = useState(plans[0]?.id ? String(plans[0].id) : '');
  const [startDate, setStartDate] = useState(todayInput());
  const [paymentMethod, setPaymentMethod] = useState('cash');
  const [amount, setAmount] = useState(plans[0]?.price != null ? String(plans[0].price) : '');
  const [autoRenew, setAutoRenew] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [history, setHistory] = useState([]);
  const [payments, setPayments] = useState([]);
  const [receipt, setReceipt] = useState(null);

  useEffect(() => {
    let active = true;
    if (!member) return () => { active = false; };

    api.get(`/members/${member.id}/memberships`)
      .then((response) => { if (active) setHistory(response.data?.data || []); })
      .catch(() => { if (active) setHistory([]); });

    api.get(`/members/${member.id}/payments`)
      .then((response) => { if (active) setPayments(response.data?.data || []); })
      .catch(() => { if (active) setPayments([]); });

    return () => { active = false; };
  }, [member]);

  const selectedPlan = useMemo(
    () => plans.find((plan) => String(plan.id) === String(planId)) || null,
    [plans, planId],
  );

  const projectedEnd = useMemo(() => {
    if (!selectedPlan?.durationDays || !startDate) return null;
    const base = new Date(`${startDate}T00:00:00`);
    if (Number.isNaN(base.getTime())) return null;
    base.setDate(base.getDate() + selectedPlan.durationDays);
    return base;
  }, [selectedPlan, startDate]);

  const changePlan = (value) => {
    setPlanId(value);
    const plan = plans.find((item) => String(item.id) === String(value));
    if (plan) setAmount(String(plan.price ?? 0));
  };

  const isCourtesy = paymentMethod === 'none';
  const statusInfo = membershipLabel[member?.membershipStatus] || membershipLabel.none;

  const save = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');

    try {
      const response = await api.post(`/members/${member.id}/membership`, {
        planId: Number(planId),
        startDate,
        paymentMethod,
        noCharge: isCourtesy,
        amount: isCourtesy ? 0 : Number(amount) || 0,
        autoRenew,
      });

      onSaved(response.data?.message || 'Membresía actualizada correctamente.');
      onClose();
    } catch (err) {
      setError(errorMessage(err, 'No se pudo asignar la membresía.'));
    } finally {
      setSaving(false);
    }
  };

  const openReceipt = async (payment) => {
    try {
      const response = await api.get(`/members/${member.id}/payments/${payment.id}/receipt`, {
        responseType: 'text',
      });
      setReceipt({ html: response.data, title: `Recibo PAGO-${payment.id}` });
    } catch {
      setError('No se pudo generar el recibo del pago.');
    }
  };

  return (
    <>
      <Modal
      isOpen={!!member}
      onClose={() => !saving && onClose()}
      title={`Membresía · ${member?.firstName || ''} ${member?.lastName || ''}`.trim()}
      size="md"
    >
      {member && (
        <form onSubmit={save} className="space-y-4">
          <div className="flex flex-wrap items-center gap-2 rounded-lg bg-slate-50 px-4 py-3 text-sm text-slate-600">
            <span className="font-medium text-slate-800">{member.email || 'Sin correo'}</span>
            <span className="text-slate-300">·</span>
            <Badge variant={statusInfo.variant} dot>{statusInfo.label}</Badge>
            {member.membershipType && (
              <>
                <span className="text-slate-300">·</span>
                <span>{member.membershipType}</span>
              </>
            )}
            {member.membershipEnd && (
              <span className="text-xs text-slate-400">
                vence {formatDate(member.membershipEnd)}
              </span>
            )}
          </div>

          {plans.length === 0 ? (
            <div className="flex items-start gap-2 rounded-lg border border-warning-200 bg-warning-50 px-3 py-2 text-sm text-warning-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>No hay planes de membresía activos. Crea un plan antes de asignar.</span>
            </div>
          ) : (
            <>
              <Select
                label="Plan"
                value={planId}
                onChange={(event) => changePlan(event.target.value)}
                options={plans.map((plan) => ({
                  value: plan.id,
                  label: `${plan.planName} — ${formatCurrency(plan.price)} · ${plan.durationDays} días`,
                }))}
                required
              />

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <Input
                  label="Inicio"
                  type="date"
                  value={startDate}
                  onChange={(event) => setStartDate(event.target.value)}
                  help={
                    member.membershipStatus === 'active' && member.membershipEnd
                      ? 'Si renuevas antes de vencer, los días restantes se conservan.'
                      : undefined
                  }
                  required
                />
                <Select
                  label="Forma de pago"
                  value={paymentMethod}
                  onChange={(event) => setPaymentMethod(event.target.value)}
                  options={paymentOptions}
                />
                <Input
                  label="Importe"
                  type="number"
                  min="0"
                  step="0.01"
                  value={amount}
                  onChange={(event) => setAmount(event.target.value)}
                  disabled={isCourtesy}
                  help={isCourtesy ? 'La activación sin cobro queda registrada como cortesía.' : undefined}
                />
                <div className="flex items-end pb-2">
                  <label className="flex items-center gap-2 text-sm text-slate-600">
                    <input
                      type="checkbox"
                      checked={autoRenew}
                      onChange={(event) => setAutoRenew(event.target.checked)}
                      className="h-4 w-4 rounded border-slate-300 text-brand-600 focus:ring-brand-500"
                    />
                    Renovación automática
                  </label>
                </div>
              </div>

              {projectedEnd && (
                <div className="flex items-center gap-2 rounded-lg border border-brand-100 bg-brand-50/60 px-3 py-2 text-xs text-brand-700">
                  <CalendarCheck className="h-4 w-4 flex-shrink-0" />
                  <span>
                    Nuevo vencimiento estimado: <strong>{formatDate(projectedEnd)}</strong>
                    {isCourtesy ? ' · sin cobro' : ` · cobro ${formatCurrency(Number(amount) || 0)}`}
                  </span>
                </div>
              )}

              {history.length > 0 && (
                <div className="rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
                  <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-400">Historial</p>
                  <div className="max-h-32 space-y-1 overflow-y-auto text-xs text-slate-600">
                    {history.slice(0, 6).map((item) => (
                      <div key={item.id} className="flex items-center justify-between gap-2">
                        <span>{item.planName}</span>
                        <span className="text-slate-400">
                          {formatDate(item.startDate)} → {formatDate(item.endDate)}
                        </span>
                        <Badge variant={item.status === 'active' ? 'success' : item.status === 'frozen' ? 'warning' : 'neutral'}>
                          {item.status}
                        </Badge>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </>
          )}

          {payments.length > 0 && (
            <div className="rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
              <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-400">Pagos recientes</p>
              <div className="max-h-40 space-y-1 overflow-y-auto text-xs text-slate-600">
                {payments.slice(0, 10).map((payment) => (
                  <div key={payment.id} className="flex flex-wrap items-center justify-between gap-2">
                    <span className="text-slate-400">{formatDateTime(payment.paymentDate)}</span>
                    <span className="flex-1 truncate">
                      {payment.planName || 'Membresía'} · {formatCurrency(payment.amount, payment.currency)}
                    </span>
                    <Badge variant={payment.paymentMethod === 'cash' ? 'success' : 'info'}>
                      {paymentMethodLabel[payment.paymentMethod] || payment.paymentMethod}
                    </Badge>
                    <Button
                      variant="ghost"
                      size="sm"
                      icon={Printer}
                      onClick={() => openReceipt(payment)}
                    >
                      Recibo
                    </Button>
                  </div>
                ))}
              </div>
            </div>
          )}

          {error && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={onClose} disabled={saving}>
              Cancelar
            </Button>
            <Button
              type="submit"
              loading={saving}
              disabled={plans.length === 0 || !planId || (!isCourtesy && (Number(amount) || 0) <= 0)}
            >
              {member.membershipStatus === 'active' ? 'Renovar membresía' : 'Asignar membresía'}
            </Button>
          </div>
        </form>
      )}
      </Modal>

      <ReceiptPreview receipt={receipt} onClose={() => setReceipt(null)} />
    </>
  );
}
