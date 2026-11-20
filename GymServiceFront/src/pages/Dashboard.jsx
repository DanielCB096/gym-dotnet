import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Users, DollarSign, Package, UserCog, Dumbbell,
  Zap, CheckCircle, Info, Plus, TrendingUp, Loader2
} from 'lucide-react';
import api from '../services/api';
import Card from '../components/ui/Card';
import Badge from '../components/ui/Badge';
import Button from '../components/ui/Button';
import { formatCurrency } from '../utils/format';

const statCards = [
  { label: 'Miembros', value: 'members', icon: Users, color: 'brand' },
  { label: 'Ventas', value: 'sales', icon: DollarSign, color: 'success' },
  { label: 'Productos', value: 'inventory', icon: Package, color: 'purple' },
  { label: 'Personal', value: 'staff', icon: UserCog, color: 'warning' },
  { label: 'Clases', value: 'classes', icon: Dumbbell, color: 'info' },
];

const colorMap = {
  brand: { bg: 'bg-brand-50', text: 'text-brand-500' },
  success: { bg: 'bg-success-50', text: 'text-success-500' },
  purple: { bg: 'bg-purple-50', text: 'text-purple-500' },
  warning: { bg: 'bg-warning-50', text: 'text-warning-500' },
  info: { bg: 'bg-info-50', text: 'text-info-500' },
};

const formatDay = (isoDate) => {
  const date = new Date(`${isoDate}T00:00:00`);
  return date.toLocaleDateString('es-MX', { weekday: 'short', day: 'numeric' });
};

const daysLeftLabel = (days) => {
  if (days <= 0) return 'Hoy';
  if (days === 1) return 'Mañana';
  return `${days} días`;
};

function TrendsSection({ trends }) {
  const revenue = Array.isArray(trends.revenue) ? trends.revenue : [];
  const access = Array.isArray(trends.access) ? trends.access : [];
  const upcoming = Array.isArray(trends.upcoming) ? trends.upcoming : [];

  const maxRevenue = Math.max(1, ...revenue.map((day) => Number(day.total) || 0));
  const maxAccess = Math.max(1, ...access.map((day) => Number(day.count) || 0));

  return (
    <div className="grid grid-cols-1 xl:grid-cols-3 gap-4 lg:gap-6 mb-6">
      <Card title="Ingresos últimos 7 días" subtitle="Ventas y cobros de membresía">
        <div className="mb-3 flex items-center gap-4 text-xs text-slate-500">
          <span className="flex items-center gap-1.5">
            <span className="h-2.5 w-2.5 rounded-sm bg-brand-500" /> Ventas
          </span>
          <span className="flex items-center gap-1.5">
            <span className="h-2.5 w-2.5 rounded-sm bg-success-400" /> Membresías
          </span>
        </div>
        <div className="flex h-36 items-stretch gap-1.5">
          {revenue.map((day) => {
            const total = Number(day.total) || 0;
            const height = Math.max(total > 0 ? 6 : 2, Math.round((total / maxRevenue) * 100));
            const salesShare = total > 0 ? (Number(day.sales) / total) * 100 : 0;

            return (
              <div
                key={day.date}
                className="flex h-full flex-1 flex-col items-center gap-2"
                title={`${formatDay(day.date)}: ${formatCurrency(total)}`}
              >
                <div className="flex w-full flex-1 items-end">
                  <div
                    className="flex w-full max-w-10 flex-col justify-end overflow-hidden rounded-md bg-slate-100"
                    style={{ height: `${height}%` }}
                  >
                    <div className="w-full bg-success-400" style={{ height: `${100 - salesShare}%` }} />
                    <div className="w-full bg-brand-500" style={{ height: `${salesShare}%` }} />
                  </div>
                </div>
                <span className="text-[11px] text-slate-400">{formatDay(day.date)}</span>
              </div>
            );
          })}
        </div>
      </Card>

      <Card title="Accesos últimos 7 días" subtitle="Entradas concedidas por día">
        <div className="flex h-36 items-stretch gap-1.5">
          {access.map((day) => {
            const count = Number(day.count) || 0;
            const height = Math.max(count > 0 ? 6 : 2, Math.round((count / maxAccess) * 100));

            return (
              <div
                key={day.date}
                className="flex h-full flex-1 flex-col items-center gap-2"
                title={`${formatDay(day.date)}: ${count} accesos`}
              >
                <div className="flex w-full flex-1 items-end">
                  <div
                    className="w-full max-w-10 rounded-md bg-info-500"
                    style={{ height: `${height}%` }}
                  />
                </div>
                <span className="text-[11px] text-slate-400">{formatDay(day.date)}</span>
              </div>
            );
          })}
        </div>
      </Card>

      <Card title="Próximos vencimientos" subtitle="Membresías que vencen en 7 días">
        {upcoming.length === 0 ? (
          <p className="py-10 text-center text-sm text-slate-400">No hay membresías por vencer.</p>
        ) : (
          <ul className="divide-y divide-slate-100">
            {upcoming.map((item) => (
              <li key={item.id} className="flex items-center justify-between gap-3 py-2.5">
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium text-slate-700">{item.memberName}</p>
                  <p className="truncate text-xs text-slate-400">
                    {item.planName || 'Sin plan'} · {formatDay(item.endDate)}
                  </p>
                </div>
                <Badge variant={item.daysLeft <= 1 ? 'danger' : 'warning'} dot>
                  {daysLeftLabel(item.daysLeft)}
                </Badge>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}

export default function Dashboard() {
  const navigate = useNavigate();
  const [stats, setStats] = useState({ members: 0, sales: 0, inventory: 0, staff: 0, classes: 0 });
  const [summary, setSummary] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    loadStats();
  }, []);

  const loadStats = async () => {
    try {
      const [members, sales, inventory, staff, classes, dashboard] = await Promise.all([
        api.get('/members').catch(() => ({ data: { data: [], pagination: { totalCount: 0 } } })),
        api.get('/sales').catch(() => ({ data: { data: [], pagination: { totalCount: 0 } } })),
        api.get('/inventory').catch(() => ({ data: { data: [], pagination: { totalCount: 0 } } })),
        api.get('/staff').catch(() => ({ data: { data: [], pagination: { totalCount: 0 } } })),
        api.get('/classes').catch(() => ({ data: { data: [], pagination: { totalCount: 0 } } })),
        api.get('/dashboard/summary').catch(() => ({ data: { data: null } })),
      ]);

      const getCount = (resp) => {
        const pagination = resp.data?.pagination;
        if (pagination && pagination.totalCount != null) return pagination.totalCount;
        const arr = resp.data?.data || resp.data?.items || [];
        return Array.isArray(arr) ? arr.length : 0;
      };

      setStats({
        members: getCount(members),
        sales: getCount(sales),
        inventory: getCount(inventory),
        staff: getCount(staff),
        classes: getCount(classes),
      });
      setSummary(dashboard.data?.data || null);
    } catch (err) {
      console.error('Error cargando estadísticas:', err);
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <Loader2 className="w-8 h-8 mx-auto text-brand-500 animate-spin mb-3" />
          <p className="text-sm text-slate-500">Cargando dashboard...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="animate-fade-in">
      {/* Page header */}
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-slate-800 tracking-tight">Dashboard</h2>
        <p className="text-sm text-slate-500 mt-1">Resumen general del sistema</p>
      </div>

      {/* Stats Grid — TailAdmin style */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-5 gap-4 lg:gap-6 mb-6">
        {statCards.map((stat) => {
          const Icon = stat.icon;
          const colors = colorMap[stat.color];
          return (
            <div
              key={stat.label}
              className="bg-white rounded-xl border border-slate-200 shadow-sm p-6"
            >
              <div className="flex items-center justify-between mb-4">
                <div className={`w-12 h-12 rounded-full ${colors.bg} ${colors.text} flex items-center justify-center`}>
                  <Icon className="w-6 h-6" />
                </div>
                <Badge variant={stat.color} dot>
                  <TrendingUp className="w-3 h-3" />
                </Badge>
              </div>
              <div className="text-2xl font-bold text-slate-800 mb-1">
                {stats[stat.value]}
              </div>
              <div className="text-sm text-slate-500">{stat.label}</div>
            </div>
          );
        })}
      </div>

      {summary && (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-5 gap-4 lg:gap-6 mb-6">
          {[
            { label: 'Ingresos hoy', value: formatCurrency(summary.salesToday + summary.paymentsToday) },
            { label: 'Membresías activas', value: summary.activeMemberships },
            { label: 'Por vencer (7 días)', value: summary.expiringSoon },
            { label: 'Accesos hoy', value: summary.accessToday },
            { label: 'Stock bajo', value: summary.lowStock },
          ].map((item) => (
            <div key={item.label} className="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-400">{item.label}</p>
              <p className="mt-2 text-xl font-bold text-slate-800">{item.value}</p>
            </div>
          ))}
        </div>
      )}

      {summary?.trends && <TrendsSection trends={summary.trends} />}

      {/* Bottom section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-4 lg:gap-6">
        {/* Quick Actions */}
        <Card
          title="Acciones rápidas"
          subtitle="Accede a las funciones más usadas"
        >
          <div className="space-y-3">
            <Button
              variant="outline"
              size="sm"
              icon={Plus}
              className="w-full justify-start"
              onClick={() => navigate('/members')}
            >
              Registrar nuevo miembro
            </Button>
            <Button
              variant="outline"
              size="sm"
              icon={Plus}
              className="w-full justify-start"
              onClick={() => navigate('/sales')}
            >
              Registrar venta
            </Button>
            <Button
              variant="outline"
              size="sm"
              icon={Plus}
              className="w-full justify-start"
              onClick={() => navigate('/inventory')}
            >
              Agregar producto
            </Button>
          </div>
        </Card>

        {/* System Status */}
        <Card title="Estado del sistema">
          <div className="space-y-4">
            {[
              { label: 'Backend conectado', status: true },
              { label: 'Base de datos activa', status: true },
              { label: 'Servicio operativo', status: true },
            ].map((item) => (
              <div key={item.label} className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <CheckCircle className={`w-4 h-4 ${item.status ? 'text-success-500' : 'text-danger-500'}`} />
                  <span className="text-sm text-slate-600">{item.label}</span>
                </div>
                <Badge variant={item.status ? 'success' : 'danger'} dot>
                  {item.status ? 'Activo' : 'Error'}
                </Badge>
              </div>
            ))}
          </div>
        </Card>

        {/* Info */}
        <Card title="Información">
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <Info className="w-4 h-4 text-info-500" />
                <span className="text-sm text-slate-600">Fecha</span>
              </div>
              <span className="text-sm font-medium text-slate-700">
                {new Date().toLocaleDateString('es-MX', {
                  day: 'numeric',
                  month: 'short',
                  year: 'numeric',
                })}
              </span>
            </div>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <Zap className="w-4 h-4 text-warning-500" />
                <span className="text-sm text-slate-600">Hora</span>
              </div>
              <span className="text-sm font-medium text-slate-700">
                {new Date().toLocaleTimeString('es-MX', {
                  hour: '2-digit',
                  minute: '2-digit',
                })}
              </span>
            </div>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-3">
                <Dumbbell className="w-4 h-4 text-brand-500" />
                <span className="text-sm text-slate-600">Versión</span>
              </div>
              <Badge variant="primary">v1.0</Badge>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
}
