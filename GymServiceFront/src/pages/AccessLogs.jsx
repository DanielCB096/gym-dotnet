import { useEffect, useState } from 'react';
import { AlertCircle, RefreshCw, Trash2, UserCheck } from 'lucide-react';
import api from '../services/api';
import useApiList from '../hooks/useApiList';
import PageHeader from '../components/ui/PageHeader';
import Table from '../components/ui/Table';
import Button from '../components/ui/Button';
import Badge from '../components/ui/Badge';
import Input from '../components/ui/Input';
import Select from '../components/ui/Select';
import Modal from '../components/ui/Modal';
import ConfirmDialog from '../components/ui/ConfirmDialog';
import { formatDateTime } from '../utils/format';

export default function AccessLogs() {
  const [filters, setFilters] = useState({ startDate: '', endDate: '' });
  const [applied, setApplied] = useState({ startDate: '', endDate: '' });
  const [notice, setNotice] = useState(null);
  const [confirmClean, setConfirmClean] = useState(false);
  const [cleaning, setCleaning] = useState(false);
  const [manualOpen, setManualOpen] = useState(false);
  const [manualForm, setManualForm] = useState({ memberId: '', reason: '' });
  const [manualSaving, setManualSaving] = useState(false);
  const [manualError, setManualError] = useState('');
  const [members, setMembers] = useState([]);

  useEffect(() => {
    let active = true;
    api.get('/members', { params: { limit: 500 } })
      .then((response) => { if (active) setMembers(response.data?.data || []); })
      .catch(() => { if (active) setMembers([]); });
    return () => { active = false; };
  }, []);

  const params = {};
  if (applied.startDate) params.startDate = applied.startDate;
  if (applied.endDate) params.endDate = applied.endDate;

  const { items, loading, error, pagination, page, setPage, reload } = useApiList('/access/logs', {
    params,
  });

  const notify = (type, message) => {
    setNotice({ type, message });
    setTimeout(() => setNotice(null), 5000);
  };

  const applyFilters = (event) => {
    event.preventDefault();
    setPage(1);
    setApplied({ ...filters });
  };

  const cleanOld = async () => {
    setCleaning(true);
    try {
      const response = await api.delete('/access/clean/90');
      notify('success', response.data?.message || 'Registros antiguos eliminados.');
      setConfirmClean(false);
      reload();
    } catch (err) {
      notify('error', err.response?.data?.message || 'No se pudieron limpiar los registros.');
    } finally {
      setCleaning(false);
    }
  };

  const openManual = () => {
    setManualForm({ memberId: '', reason: '' });
    setManualError('');
    setManualOpen(true);
  };

  const submitManual = async (event) => {
    event.preventDefault();
    setManualSaving(true);
    setManualError('');
    try {
      const response = await api.post('/access/manual', {
        memberId: Number(manualForm.memberId),
        reason: manualForm.reason,
        deviceId: 'recepcion',
      });
      notify('success', response.data?.message || 'Acceso manual registrado.');
      setManualOpen(false);
      reload();
    } catch (err) {
      setManualError(err.response?.data?.message || 'No se pudo registrar el acceso manual.');
    } finally {
      setManualSaving(false);
    }
  };

  const columns = [
    { header: 'Socio', accessor: 'memberName' },
    { header: 'Tipo', accessor: 'accessType' },
    {
      header: 'Resultado',
      accessor: 'accessResult',
      render: (value) => (
        <Badge variant={value === 'granted' ? 'success' : 'danger'} dot>
          {value === 'granted' ? 'Concedido' : 'Denegado'}
        </Badge>
      ),
    },
    { header: 'Dispositivo', accessor: 'deviceId' },
    { header: 'Fecha y hora', accessor: 'accessedAt', render: (value) => formatDateTime(value) },
  ];

  return (
    <div className="animate-fade-in">
      <PageHeader
        title="Logs de acceso"
        subtitle="Historial de entradas al gimnasio"
        actions={
          <>
            <Button variant="outline" icon={RefreshCw} onClick={reload} disabled={loading}>
              Actualizar
            </Button>
            <Button variant="outline" icon={UserCheck} onClick={openManual}>
              Acceso manual
            </Button>
            <Button variant="danger" icon={Trash2} onClick={() => setConfirmClean(true)}>
              Limpiar +90 días
            </Button>
          </>
        }
      />

      {notice && (
        <div
          className={`mb-4 flex items-start gap-2.5 rounded-xl border px-4 py-3 text-sm ${
            notice.type === 'success'
              ? 'bg-success-50 border-success-200 text-success-700'
              : 'bg-danger-50 border-danger-200 text-danger-700'
          }`}
        >
          <AlertCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
          <span>{notice.message}</span>
        </div>
      )}

      <form onSubmit={applyFilters} className="flex flex-col sm:flex-row sm:items-end gap-3 mb-4">
        <Input
          id="startDate"
          label="Desde"
          type="date"
          className="sm:w-48"
          value={filters.startDate}
          onChange={(event) => setFilters({ ...filters, startDate: event.target.value })}
        />
        <Input
          id="endDate"
          label="Hasta"
          type="date"
          className="sm:w-48"
          value={filters.endDate}
          onChange={(event) => setFilters({ ...filters, endDate: event.target.value })}
        />
        <Button type="submit" variant="outline">
          Filtrar
        </Button>
      </form>

      {error && (
        <div className="mb-4 flex items-start gap-2.5 rounded-xl border border-danger-200 bg-danger-50 px-4 py-3 text-sm text-danger-700">
          <AlertCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <Table
        columns={columns}
        data={items}
        loading={loading}
        emptyText="No hay registros de acceso en el periodo seleccionado"
      />

      {pagination && pagination.totalPages > 1 && (
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mt-4">
          <p className="text-sm text-slate-500">
            Página {page} de {pagination.totalPages} · {pagination.totalCount} registros
          </p>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={!pagination.hasPrev || loading}
              onClick={() => setPage(page - 1)}
            >
              Anterior
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={!pagination.hasNext || loading}
              onClick={() => setPage(page + 1)}
            >
              Siguiente
            </Button>
          </div>
        </div>
      )}

      <Modal isOpen={manualOpen} onClose={() => !manualSaving && setManualOpen(false)} title="Acceso manual" size="md">
        <form onSubmit={submitManual} className="space-y-4">
          <Select
            label="Socio"
            value={manualForm.memberId}
            onChange={(event) => setManualForm({ ...manualForm, memberId: event.target.value })}
            options={members.map((member) => ({
              value: member.id,
              label: `${member.firstName} ${member.lastName}${member.email ? ` · ${member.email}` : ''}`,
            }))}
            placeholder="Selecciona un socio"
            required
          />
          <div>
            <label htmlFor="manualReason" className="block text-sm font-medium text-slate-700 mb-1.5">
              Motivo
            </label>
            <textarea
              id="manualReason"
              rows={3}
              required
              value={manualForm.reason}
              onChange={(event) => setManualForm({ ...manualForm, reason: event.target.value })}
              placeholder="Ej. visita, cortesía, excepción autorizada…"
              className="block w-full rounded-lg border border-slate-300 bg-white text-sm text-slate-800 placeholder-slate-400 px-3.5 py-2.5 focus:outline-none focus:ring-2 focus:ring-brand-500/50 focus:border-brand-500"
            />
          </div>

          {manualError && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{manualError}</span>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={() => setManualOpen(false)} disabled={manualSaving}>
              Cancelar
            </Button>
            <Button type="submit" icon={UserCheck} loading={manualSaving} disabled={!manualForm.memberId || !manualForm.reason.trim()}>
              Registrar acceso
            </Button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={confirmClean}
        onClose={() => setConfirmClean(false)}
        onConfirm={cleanOld}
        loading={cleaning}
        title="Limpiar registros antiguos"
        message="Se eliminarán los registros de acceso con más de 90 días. Esta acción no se puede deshacer."
        confirmLabel="Eliminar registros"
      />
    </div>
  );
}
