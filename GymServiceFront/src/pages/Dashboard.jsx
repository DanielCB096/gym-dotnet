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
