import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AlertTriangle, CheckCircle2, Package, Users } from 'lucide-react';
import api from '../services/api';
import Modal from './ui/Modal';
import Button from './ui/Button';

export default function NotificationsModal({ open, onClose }) {
  const navigate = useNavigate();
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!open) return undefined;
    let active = true;
    setLoading(true);
    setError('');
    api.get('/dashboard/summary')
      .then((response) => {
        if (active) setSummary(response.data?.data || null);
      })
      .catch((err) => {
        if (active) setError(err.response?.data?.message || 'No se pudieron cargar las notificaciones.');
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => { active = false; };
  }, [open]);

  const go = (path) => {
    onClose();
    navigate(path);
  };

  const expiring = Number(summary?.expiringSoon || 0);
  const lowStock = Number(summary?.lowStock || 0);
  const hasAlerts = expiring > 0 || lowStock > 0;

  return (
    <Modal isOpen={open} onClose={onClose} title="Notificaciones" size="md">
      {loading ? (
        <p className="text-sm text-slate-500">Cargando...</p>
      ) : error ? (
        <p className="text-sm text-danger-600">{error}</p>
      ) : !hasAlerts ? (
        <div className="flex items-center gap-3 rounded-lg border border-success-200 bg-success-50 p-4 text-sm text-success-700">
          <CheckCircle2 className="h-5 w-5 flex-shrink-0" />
          Todo al día: no hay membresías por vencer ni productos con stock bajo.
        </div>
      ) : (
        <div className="space-y-3">
          {expiring > 0 && (
            <button
              type="button"
              onClick={() => go('/members')}
              className="flex w-full items-start gap-3 rounded-lg border border-warning-200 bg-warning-50 p-4 text-left hover:bg-warning-100 transition-colors"
            >
              <Users className="mt-0.5 h-5 w-5 flex-shrink-0 text-warning-600" />
              <span>
                <span className="block text-sm font-semibold text-warning-800">
                  {expiring} membresía{expiring === 1 ? '' : 's'} por vencer
                </span>
                <span className="block text-xs text-warning-700">
                  Vencen en los próximos días. Revisa y contacta a los socios.
                </span>
              </span>
            </button>
          )}

          {lowStock > 0 && (
            <button
              type="button"
              onClick={() => go('/inventory')}
              className="flex w-full items-start gap-3 rounded-lg border border-danger-200 bg-danger-50 p-4 text-left hover:bg-danger-100 transition-colors"
            >
              <Package className="mt-0.5 h-5 w-5 flex-shrink-0 text-danger-600" />
              <span>
                <span className="block text-sm font-semibold text-danger-800">
                  {lowStock} producto{lowStock === 1 ? '' : 's'} con stock bajo
                </span>
                <span className="block text-xs text-danger-700">
                  Revisa el inventario y programa una compra.
                </span>
              </span>
            </button>
          )}

          <div className="flex items-start gap-2 rounded-lg bg-slate-50 p-3 text-xs text-slate-500">
            <AlertTriangle className="mt-0.5 h-4 w-4 flex-shrink-0" />
            Las alertas se calculan con el resumen del dashboard al abrir esta ventana.
          </div>
        </div>
      )}

      <div className="mt-4 flex justify-end">
        <Button variant="ghost" onClick={onClose}>Cerrar</Button>
      </div>
    </Modal>
  );
}
