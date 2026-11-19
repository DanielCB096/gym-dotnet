import { useState, useEffect } from 'react';
import { NavLink, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import {
  LayoutDashboard, Users, Package, DollarSign, Dumbbell, UserCog,
  FileText, Landmark, Wrench, Activity, ClipboardCheck, Monitor,
  BarChart3, Shield, LogOut, Menu, X, ChevronLeft, ChevronRight,
  Bell, Search, Home, Tags, Wallet, Truck
} from 'lucide-react';

const menuSections = [
  {
    title: 'MENU',
    items: [
      { to: '/', label: 'Dashboard', icon: LayoutDashboard },
    ],
  },
  {
    title: 'MÓDULOS',
    items: [
      { to: '/members', label: 'Miembros', icon: Users },
      { to: '/plans', label: 'Planes', icon: Tags },
      { to: '/inventory', label: 'Inventario', icon: Package },
      { to: '/purchases', label: 'Compras', icon: Truck },
      { to: '/sales', label: 'Ventas', icon: DollarSign },
      { to: '/cash', label: 'Caja', icon: Wallet },
      { to: '/classes', label: 'Clases', icon: Dumbbell },
      { to: '/staff', label: 'Personal', icon: UserCog },
      { to: '/billing', label: 'Facturación', icon: FileText },
      { to: '/finance', label: 'Finanzas', icon: Landmark },
      { to: '/maintenance', label: 'Mantenimiento', icon: Wrench },
      { to: '/fisio', label: 'Fisioterapia', icon: Activity },
      { to: '/routines', label: 'Rutinas', icon: ClipboardCheck },
      { to: '/kiosk', label: 'Kioscos', icon: Monitor },
      { to: '/reports', label: 'Reportes', icon: BarChart3 },
      { to: '/access-logs', label: 'Logs Acceso', icon: Shield },
    ],
  },
];

// Map routes to labels for breadcrumb
const routeLabels = {
  '/': 'Dashboard',
  '/members': 'Miembros',
  '/plans': 'Planes',
  '/inventory': 'Inventario',
  '/purchases': 'Compras',
  '/sales': 'Ventas',
  '/cash': 'Caja',
  '/classes': 'Clases',
  '/staff': 'Personal',
  '/billing': 'Facturación',
  '/finance': 'Finanzas',
  '/maintenance': 'Mantenimiento',
  '/fisio': 'Fisioterapia',
  '/routines': 'Rutinas',
  '/kiosk': 'Kioscos',
  '/reports': 'Reportes',
  '/access-logs': 'Logs Acceso',
};

export default function Layout({ children }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  // Persist sidebar state in localStorage
  const [sidebarOpen, setSidebarOpen] = useState(() => {
    const stored = localStorage.getItem('sidebar-collapsed');
    return stored ? stored === 'false' : true;
  });
  const [mobileOpen, setMobileOpen] = useState(false);

  // Save sidebar state to localStorage whenever it changes
  useEffect(() => {
    localStorage.setItem('sidebar-collapsed', String(!sidebarOpen));
  }, [sidebarOpen]);

  // Close the mobile drawer when navigating
  useEffect(() => {
    setMobileOpen(false);
  }, [location.pathname]);

  // En móvil el drawer siempre se abre expandido, aunque el sidebar
  // estuviera colapsado en escritorio.
  const expanded = sidebarOpen || mobileOpen;

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const currentPageLabel = routeLabels[location.pathname] || 'Dashboard';

  return (
    <div className="min-h-screen bg-slate-100">
      {/* Mobile overlay */}
      {mobileOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-40 lg:hidden"
          onClick={() => setMobileOpen(false)}
        />
      )}

      {/* Sidebar — 288px expanded / 80px collapsed */}
      <aside
        className={`fixed top-0 left-0 z-50 flex h-full flex-col bg-[#1E293B] text-white transition-all duration-300 ease-in-out
          ${expanded ? 'w-72' : 'w-20'}
          ${mobileOpen ? 'translate-x-0' : '-translate-x-full'}
          lg:translate-x-0`}
        style={{ overflow: 'hidden' }}
      >
        {/* Logo */}
        <div className="flex h-20 flex-shrink-0 items-center justify-between border-b border-white/5 px-4">
          {expanded ? (
            <NavLink to="/" className="flex items-center gap-3" onClick={() => setMobileOpen(false)}>
              <div className="w-9 h-9 bg-brand-500 rounded-lg flex items-center justify-center shadow-lg shadow-brand-500/30">
                <Dumbbell className="w-5 h-5 text-white" />
              </div>
              <span className="font-bold text-lg tracking-tight">GymService</span>
            </NavLink>
          ) : (
            <NavLink to="/" className="flex items-center justify-center w-9 h-9 bg-brand-500 rounded-lg shadow-lg shadow-brand-500/30">
              <Dumbbell className="w-5 h-5 text-white" />
            </NavLink>
          )}
          {/* Mobile close */}
          <button
            onClick={() => setMobileOpen(false)}
            aria-label="Cerrar menú lateral"
            className="lg:hidden p-2 rounded-lg hover:bg-sidebar-700 transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Navigation */}
        <nav className="flex-1 overflow-y-auto px-3 py-4">
          {menuSections.map((section, sIdx) => (
            <div key={sIdx} className="mb-6 last:mb-0">
              {expanded && section.title && (
                <div className="mb-2 px-3">
                  <span className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
                    {section.title}
                  </span>
                </div>
              )}
              <div className="space-y-1">
                {section.items.map((item) => {
                  const Icon = item.icon;
                  return (
                    <NavLink
                      key={item.to}
                      to={item.to}
                      onClick={() => setMobileOpen(false)}
                      className={({ isActive }) =>
                        `group flex min-h-10 items-center gap-3 rounded-lg px-3 py-2 transition-colors duration-150
                          ${isActive
                            ? 'bg-brand-500/15 font-medium text-white ring-1 ring-inset ring-brand-400/30'
                            : 'text-slate-300 hover:bg-white/5 hover:text-white'
                          }
                          ${!expanded ? 'justify-center' : ''}`
                      }
                      title={!expanded ? item.label : undefined}
                    >
                      {({ isActive }) => (
                        <>
                          <Icon className={`h-5 w-5 flex-shrink-0 ${isActive ? 'text-brand-300' : 'text-slate-400 group-hover:text-slate-200'}`} />
                          {expanded && (
                            <span className="truncate text-sm">{item.label}</span>
                          )}
                        </>
                      )}
                    </NavLink>
                  );
                })}
              </div>
            </div>
          ))}
        </nav>

        {/* Sidebar footer */}
        <div className="flex-shrink-0 border-t border-white/5 bg-black/10 p-3">
          {expanded ? (
            <div className="space-y-3">
              <div className="flex items-center gap-3 px-1">
                <div className="flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-full bg-brand-500 text-sm font-bold shadow">
                  {(user?.fullName || user?.username || 'U').charAt(0).toUpperCase()}
                </div>
                <div className="min-w-0 flex-1">
                  <div className="truncate text-sm font-medium text-white">
                    {user?.fullName || user?.username || 'Usuario'}
                  </div>
                  <div className="truncate text-xs text-slate-400">
                    {user?.role || 'Admin'}
                  </div>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <button
                  onClick={handleLogout}
                  aria-label="Cerrar sesión"
                  className="flex flex-1 items-center justify-center gap-2 rounded-lg px-3 py-2 text-sm text-danger-300 transition-colors hover:bg-danger-500/10 hover:text-danger-200"
                >
                  <LogOut className="h-4 w-4" />
                  Salir
                </button>
                <button
                  onClick={() => setSidebarOpen(false)}
                  aria-label="Colapsar menú lateral"
                  className="hidden items-center justify-center rounded-lg p-2 text-slate-400 transition-colors hover:bg-white/5 hover:text-white lg:flex"
                >
                  <ChevronLeft className="h-4 w-4" />
                </button>
              </div>
            </div>
          ) : (
            <div className="flex flex-col items-center gap-2">
              <button
                onClick={handleLogout}
                aria-label="Cerrar sesión"
                className="rounded-lg p-2 text-slate-400 transition-colors hover:bg-danger-500/10 hover:text-danger-300"
              >
                <LogOut className="h-5 w-5" />
              </button>
              <button
                onClick={() => setSidebarOpen(true)}
                aria-label="Expandir menú lateral"
                className="hidden rounded-lg p-2 text-slate-400 transition-colors hover:bg-white/5 hover:text-white lg:flex"
              >
                <ChevronRight className="h-5 w-5" />
              </button>
            </div>
          )}
        </div>
      </aside>

      {/* Main content */}
      <main
        className={`transition-all duration-300 ease-in-out min-h-screen
          ${sidebarOpen ? 'lg:ml-72' : 'lg:ml-20'}`}
      >
        {/* Header — 80px tall */}
        <header className="sticky top-0 z-30 border-b border-slate-200 bg-white/95 backdrop-blur supports-[backdrop-filter]:bg-white/80">
          <div className="flex h-16 items-center justify-between gap-3 px-4 sm:px-6 lg:h-20 lg:px-8">
            {/* Left: Breadcrumb */}
            <div className="flex items-center gap-2 min-w-0 flex-1">
              <button
                onClick={() => setMobileOpen(true)}
                aria-label="Abrir menú lateral"
                className="lg:hidden p-2 -ml-2 rounded-lg hover:bg-slate-100 text-slate-600 transition-colors flex-shrink-0"
              >
                <Menu className="w-5 h-5" />
              </button>
              <nav className="flex items-center gap-1.5 text-sm min-w-0">
                <Home className="w-4 h-4 text-slate-400 flex-shrink-0" />
                <span className="text-slate-400 hidden sm:inline">/</span>
                <span className="font-medium text-slate-700 truncate">{currentPageLabel}</span>
              </nav>
            </div>

            {/* Right: Search, Notifications, User */}
            <div className="flex items-center gap-1 lg:gap-2 flex-shrink-0">
              {/* Search */}
              <div className="hidden md:flex items-center gap-2 px-3 py-2 rounded-lg border border-slate-200 bg-slate-50 w-48 lg:w-64">
                <Search className="w-4 h-4 text-slate-400 flex-shrink-0" />
                <input
                  type="text"
                  placeholder="Buscar..."
                  className="bg-transparent text-sm text-slate-600 placeholder-slate-400 outline-none w-full"
                />
              </div>

              {/* Notifications */}
              <button className="relative p-2 rounded-lg hover:bg-slate-100 text-slate-500 transition-colors flex-shrink-0" aria-label="Notificaciones">
                <Bell className="w-5 h-5" />
                <span className="absolute top-1.5 right-1.5 w-2 h-2 bg-danger-500 rounded-full ring-2 ring-white"></span>
              </button>

              {/* User avatar */}
              <div className="hidden sm:flex items-center gap-2 pl-2 sm:pl-3 ml-1 sm:ml-1 border-l border-slate-200">
                <div className="w-8 h-8 rounded-full bg-brand-500 flex items-center justify-center text-xs font-bold text-white flex-shrink-0">
                  {(user?.fullName || user?.username || 'U').charAt(0).toUpperCase()}
                </div>
                <div className="hidden lg:block min-w-0">
                  <div className="text-sm font-medium text-slate-700 truncate" style={{ maxWidth: '120px' }}>
                    {user?.fullName || user?.username}
                  </div>
                  <div className="text-xs text-slate-400 truncate">
                    {user?.role || 'Admin'}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </header>

        {/* Page content */}
        <div className="mx-auto w-full max-w-[1600px] p-4 sm:p-6 lg:p-8">
          {children}
        </div>
      </main>
    </div>
  );
}
