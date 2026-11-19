import { useState } from 'react';
import { AlertCircle } from 'lucide-react';
import api from '../services/api';
import Modal from './ui/Modal';
import Button from './ui/Button';

const errorMessage = (err, fallback) => err?.response?.data?.message || fallback;

export default function CancelMembershipModal({ target, onClose, onCancelled }) {
  const [reason, setReason] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const save = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');

    try {
      const response = await api.post(`/memberships/${target.member.membershipId}/cancel`, {
        reason: reason.trim(),
      });
      onCancelled(response.data?.message || 'Membresía cancelada.');
      onClose();
    } catch (err) {
      setError(errorMessage(err, 'No se pudo cancelar la membresía.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      isOpen={!!target}
      onClose={() => !saving && onClose()}
      title={`Cancelar membresía · ${target?.member?.firstName || ''} ${target?.member?.lastName || ''}`.trim()}
      size="sm"
    >
      {target && (
        <form onSubmit={save} className="space-y-4">
          <div className="rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
            La membresía quedará cancelada y el socio perderá el acceso. El historial se conserva.
          </div>

          <div>
            <label htmlFor="cancelReason" className="block text-sm font-medium text-slate-700 mb-1.5">
              Motivo de cancelación
            </label>
            <textarea
              id="cancelReason"
              rows={3}
              required
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Ej. cambio de gimnasio, lesión, solicitud del socio…"
              className="block w-full rounded-lg border border-slate-300 bg-white text-sm text-slate-800 placeholder-slate-400 px-3.5 py-2.5 focus:outline-none focus:ring-2 focus:ring-danger-500/50 focus:border-danger-500"
            />
          </div>

          {error && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={onClose} disabled={saving}>
              Volver
            </Button>
            <Button type="submit" variant="danger" loading={saving} disabled={!reason.trim()}>
              Cancelar membresía
            </Button>
          </div>
        </form>
      )}
    </Modal>
  );
}
