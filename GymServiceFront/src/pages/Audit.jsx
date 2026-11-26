import { useCallback, useEffect, useMemo, useState } from 'react';
import { AlertCircle, History, LogIn, RefreshCw, ShieldAlert, ShieldCheck } from 'lucide-react';
import api from '../services/api';
import useApiList from '../hooks/useApiList';
import PageHeader from '../components/ui/PageHeader';
import Card from '../components/ui/Card';
import Table from '../components/ui/Table';
import Button from '../components/ui/Button';
import Badge from '../components/ui/Badge';
import Input from '../components/ui/Input';
import Select from '../components/ui/Select';
import { formatDateTime } from '../utils/format';

const sessionStates = [
  { value: '', label: 'Todas las sesiones' },
  { value: 'active', label: 'Activas' },
  { value: 'expired', label: 'Expiradas' },
  { value: 'revoked', label: 'Revocadas' },
];

const accessResults = [
  { value: '', label: 'Todos los resultados' },
  { value: 'granted', label: 'Concedidos' },
  { value: 'denied', label: 'Denegados' },
];

const stateBadge = {
  active: { variant: 'success', label: 'Activa' },
  expired: { variant: 'warning', label: 'Expirada' },
  revoked: { variant: 'danger', label: 'Revocada' },
};

function SummaryCard({ icon: Icon, label, value, tone }) {
  return (
    <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex items-center justify-between gap-3">
        <div>
          <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{label}</p>
          <p className="mt-1 text-2xl font-bold text-slate-800">{value}</p>
        </div>
        <span className={`flex h-10 w-10 items-center justify-center rounded-lg ${tone}`}>
          <Icon className="h-5 w-5" />
        </span>
      </div>
    </div>
  );
}

function Pagination({ pagination, page, setPage, loading }) {
  if (!pagination || pagination.totalPages <= 1) return null;

  return (
    <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
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
  );
}

export default function Audit() {
  const [tab, setTab] = useState('sessions');
  const [summary, setSummary] = useState(null);
  const [summaryError, setSummaryError] = useState('');
  const [summaryLoading, setSummaryLoading] = useState(false);

  const [sessionFilters, setSessionFilters] = useState({ query: '', state: '' });
  const [sessionApplied, setSessionApplied] = useState({ query: '', state: '' });

  const [accessFilters, setAccessFilters] = useState({ query: '', result: '', startDate: '', endDate: '' });
  const [accessApplied, setAccessApplied] = useState({ query: '', result: '', startDate: '', endDate: '' });

  const loadSummary = useCallback(async () => {
    setSummaryLoading(true);
    setSummaryError('');
    try {
      const response = await api.get('/audit/summary');
      setSummary(response.data?.data || null);
    } catch (err) {
      setSummaryError(err.response?.data?.message || 'No se pudo cargar el resumen de auditoría.');
    } finally {
      setSummaryLoading(false);
    }
  }, []);

  useEffect(() => {
    let active = true;
    Promise.resolve().then(() => {
      if (active) loadSummary();
    });
    return () => {
      active = false;
    };
  }, [loadSummary]);

  const sessionParams = useMemo(() => {
    const params = {};
    if (sessionApplied.query.trim()) params.query = sessionApplied.query.trim();
    if (sessionApplied.state) params.state = sessionApplied.state;
    return params;
  }, [sessionApplied]);

  const accessParams = useMemo(() => {
    const params = {};
    if (accessApplied.query.trim()) params.query = accessApplied.query.trim();
    if (accessApplied.result) params.result = accessApplied.result;
    if (accessApplied.startDate) params.startDate = accessApplied.startDate;
    if (accessApplied.endDate) params.endDate = accessApplied.endDate;
    return params;
  }, [accessApplied]);

  const sessions = useApiList('/audit/sessions', {
    params: sessionParams,
    enabled: tab === 'sessions',
  });

  const accesses = useApiList('/audit/access', {
    params: accessParams,
    enabled: tab === 'access',
  });

  const refresh = () => {
    loadSummary();
    if (tab === 'sessions') sessions.reload();
    else accesses.reload();
  };

  const applySessionFilters = (event) => {
    event.preventDefault();
    sessions.setPage(1);
    setSessionApplied({ ...sessionFilters });
  };

  const applyAccessFilters = (event) => {
    event.preventDefault();
    accesses.setPage(1);
    setAccessApplied({ ...accessFilters });
  };

  const sessionColumns = [
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
    { header: 'Rol', accessor: 'role', render: (value) => value || '—' },
    { header: 'IP', accessor: 'ipAddress', render: (value) => value || '—' },
    {
      header: 'Dispositivo',
      accessor: 'deviceInfo',
      render: (value) => (
        <span className="block max-w-[180px] truncate text-slate-500" title={value || ''}>
          {value || '—'}
        </span>
      ),
    },
    { header: 'Inicio', accessor: 'createdAt', render: (value) => formatDateTime(value) },
    { header: 'Expira', accessor: 'expiresAt', render: (value) => formatDateTime(value) },
    {
      header: 'Estado',
      accessor: 'state',
      render: (value) => {
        const meta = stateBadge[value] || stateBadge.expired;
        return <Badge variant={meta.variant} dot>{meta.label}</Badge>;
      },
    },
  ];

  const accessColumns = [
    { header: 'Socio', accessor: 'memberName', render: (value) => value || '—' },
    { header: 'Tipo', accessor: 'accessType', render: (value) => value || '—' },
    {
      header: 'Resultado',
      accessor: 'accessResult',
      render: (value) => (
        <Badge variant={value === 'granted' ? 'success' : 'danger'} dot>
          {value === 'granted' ? 'Concedido' : 'Denegado'}
        </Badge>
      ),
    },
    { header: 'Dispositivo', accessor: 'deviceId', render: (value) => value || '—' },
    {
      header: 'Motivo',
      accessor: 'reason',
      render: (value) => (
        <span className="block max-w-[220px] truncate text-slate-500" title={value || ''}>
          {value || '—'}
        </span>
      ),
    },
    { header: 'Fecha y hora', accessor: 'accessedAt', render: (value) => formatDateTime(value) },
  ];

  const stats = [
    { label: 'Sesiones activas', value: summary?.activeSessions ?? '—', icon: ShieldCheck, tone: 'bg-success-50 text-success-600' },
    { label: 'Inicios en 24 h', value: summary?.logins24h ?? '—', icon: LogIn, tone: 'bg-brand-50 text-brand-600' },
    { label: 'Accesos concedidos hoy', value: summary?.accessGrantedToday ?? '—', icon: History, tone: 'bg-info-50 text-info-600' },
    { label: 'Accesos denegados hoy', value: summary?.accessDeniedToday ?? '—', icon: ShieldAlert, tone: 'bg-danger-50 text-danger-600' },
  ];

  return (
    <div className="animate-fade-in">
      <PageHeader
        title="Auditoría"
        subtitle="Sesiones de usuario y accesos al gimnasio"
        actions={
          <Button variant="outline" icon={RefreshCw} onClick={refresh} disabled={summaryLoading}>
            Actualizar
          </Button>
        }
      />

      {summaryError && (
        <div className="mb-4 flex items-start gap-2.5 rounded-xl border border-danger-200 bg-danger-50 px-4 py-3 text-sm text-danger-700">
          <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
          <span>{summaryError}</span>
        </div>
      )}

      <div className="mb-6 grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {stats.map((stat) => (
          <SummaryCard key={stat.label} {...stat} />
        ))}
      </div>

      <div className="mb-4 flex gap-1 rounded-lg border border-slate-200 bg-slate-50 p-1">
        <button
          type="button"
          onClick={() => setTab('sessions')}
          className={`flex-1 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
            tab === 'sessions' ? 'bg-white text-brand-600 shadow-sm' : 'text-slate-500 hover:text-slate-700'
          }`}
        >
          Sesiones de usuario
        </button>
        <button
          type="button"
          onClick={() => setTab('access')}
          className={`flex-1 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
            tab === 'access' ? 'bg-white text-brand-600 shadow-sm' : 'text-slate-500 hover:text-slate-700'
          }`}
        >
          Accesos al gimnasio
        </button>
      </div>

      {tab === 'sessions' ? (
        <Card title="Sesiones" subtitle="Inicios de sesión registrados, activos y revocados">
          <form onSubmit={applySessionFilters} className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-end">
            <Input
              id="sessionQuery"
              label="Buscar"
              placeholder="Usuario, IP o dispositivo"
              value={sessionFilters.query}
              onChange={(event) => setSessionFilters({ ...sessionFilters, query: event.target.value })}
              className="sm:flex-1"
            />
            <Select
              label="Estado"
              value={sessionFilters.state}
              onChange={(event) => setSessionFilters({ ...sessionFilters, state: event.target.value })}
              options={sessionStates}
              className="sm:w-52"
            />
            <Button type="submit" variant="outline">Filtrar</Button>
          </form>

          {sessions.error && (
            <div className="mb-4 flex items-start gap-2.5 rounded-xl border border-danger-200 bg-danger-50 px-4 py-3 text-sm text-danger-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{sessions.error}</span>
            </div>
          )}

          <Table
            columns={sessionColumns}
            data={sessions.items}
            loading={sessions.loading}
            emptyText="No hay sesiones para los filtros seleccionados"
          />

          <Pagination
            pagination={sessions.pagination}
            page={sessions.page}
            setPage={sessions.setPage}
            loading={sessions.loading}
          />
        </Card>
      ) : (
        <Card title="Accesos" subtitle="Entradas concedidas y denegadas en el gimnasio">
          <form onSubmit={applyAccessFilters} className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-end">
            <Input
              id="accessQuery"
              label="Buscar"
              placeholder="Socio, motivo o dispositivo"
              value={accessFilters.query}
              onChange={(event) => setAccessFilters({ ...accessFilters, query: event.target.value })}
              className="lg:flex-1"
            />
            <Select
              label="Resultado"
              value={accessFilters.result}
              onChange={(event) => setAccessFilters({ ...accessFilters, result: event.target.value })}
              options={accessResults}
              className="lg:w-52"
            />
            <Input
              id="accessStartDate"
              label="Desde"
              type="date"
              value={accessFilters.startDate}
              onChange={(event) => setAccessFilters({ ...accessFilters, startDate: event.target.value })}
              className="lg:w-44"
            />
            <Input
              id="accessEndDate"
              label="Hasta"
              type="date"
              value={accessFilters.endDate}
              onChange={(event) => setAccessFilters({ ...accessFilters, endDate: event.target.value })}
              className="lg:w-44"
            />
            <Button type="submit" variant="outline">Filtrar</Button>
          </form>

          {accesses.error && (
            <div className="mb-4 flex items-start gap-2.5 rounded-xl border border-danger-200 bg-danger-50 px-4 py-3 text-sm text-danger-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{accesses.error}</span>
            </div>
          )}

          <Table
            columns={accessColumns}
            data={accesses.items}
            loading={accesses.loading}
            emptyText="No hay accesos para los filtros seleccionados"
          />

          <Pagination
            pagination={accesses.pagination}
            page={accesses.page}
            setPage={accesses.setPage}
            loading={accesses.loading}
          />
        </Card>
      )}
    </div>
  );
}
