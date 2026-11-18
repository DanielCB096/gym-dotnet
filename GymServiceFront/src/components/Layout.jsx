import { useState, useEffect } from 'react';
import { NavLink, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import {
  LayoutDashboard, Users, Package, DollarSign, Dumbbell, UserCog,
  FileText, Landmark, Wrench, Activity, ClipboardCheck, Monitor,
  BarChart3, Shield, LogOut, Menu, X, ChevronLeft, ChevronRight,
  Bell, Search, Home
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
      { to: '/inventory', label: 'Inventario', icon: Package },
      { to: '/sales', label: 'Ventas', icon: DollarSign },
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
  '/inventory': 'Inventario',
  '/sales': 'Ventas',
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
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [mobileOpen, setMobileOpen] = useState(false);

  // Close sidebar on small screens by default
  useEffect(() => {
    const handleResize = () => {
      if (window.innerWidth < 1024) {
        setSidebarOpen(false);
      }
    };
    handleResize();
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  useEffect(() => {
    setMobileOpen(false);
  }, [location.pathname]);

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
        className={`fixed top-0 left-0 h-full bg-[#1E293B] text-white z-50 transition-all duration-300 ease-in-out
          ${sidebarOpen ? 'w-72' : 'w-20'}
          ${mobileOpen ? 'translate-x-0' : '-translate-x-full'}
          lg:translate-x-0`}
        style={{ overflow: 'hidden' }}
      >
        {/* Logo */}
        <div className="flex items-center justify-between px-4 h-20 border-b border-sidebar-700">
          {sidebarOpen ? (
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
            className="lg:hidden p-2 rounded-lg hover:bg-sidebar-700 transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Navigation */}
        <nav className="mt-4 px-3 overflow-y-auto h-[calc(100vh-16rem)]">
          {menuSections.map((section, sIdx) => (
            <div key={sIdx} className="mb-4">
              {sidebarOpen && section.title && (
                <div className="px-3 mb-2">
                  <span className="text-[11px] font-semibold text-slate-500 uppercase tracking-wider">
                    {section.title}
                  </span>
                </div>
              )}
              <div className="space-y-0.5">
                {section.items.map((item) => {
                  const Icon = item.icon;
                  return (
                    <NavLink
                      key={item.to}
                      to={item.to}
                      onClick={() => setMobileOpen(false)}
                      className={({ isActive }) =>
                        `flex items-center gap-3 px-3 py-2.5 rounded-lg transition-all duration-200 group
                          ${isActive
                            ? 'bg-brand-500 text-white shadow-md shadow-brand-500/20'
                            : 'text-slate-300 hover:bg-sidebar-700 hover:text-white'
                          }
                          ${!sidebarOpen ? 'justify-center' : ''}`
                      }
                      title={!sidebarOpen ? item.label : undefined}
                    >
                      <Icon className="w-5 h-5 flex-shrink-0" />
                      {sidebarOpen && (
                        <span className="text-sm font-medium">{item.label}</span>
                      )}
                    </NavLink>
                  );
                })}
              </div>
            </div>
          ))}
        </nav>

        {/* Sidebar toggle (bottom) */}
        <div className="absolute bottom-0 left-0 right-0 border-t border-sidebar-700 bg-sidebar-800/50">
          {sidebarOpen ? (
            <div className="p-3">
              <div className="flex items-center gap-3 mb-3 px-1">
                <div className="w-9 h-9 rounded-full bg-brand-500 flex items-center justify-center text-sm font-bold shadow">
                  {(user?.fullName || user?.username || 'U').charAt(0).toUpperCase()}
                </div>
                <div className="min-w-0 flex-1">
                  <div className="text-sm font-medium text-white truncate">
                    {user?.fullName || user?.username || 'Usuario'}
                  </div>
                  <div className="text-xs text-slate-400 truncate">
                    {user?.role || 'Admin'}
                  </div>
                </div>
              </div>
              <div className="flex gap-2">
                <button
                  onClick={handleLogout}
                  className="flex-1 flex items-center justify-center gap-2 px-3 py-2 text-sm text-danger-400 hover:text-danger-300 hover:bg-danger-500/10 rounded-lg transition-colors"
                >
                  <LogOut className="w-4 h-4" />
                  Salir
                </button>
                <button
                  onClick={() => setSidebarOpen(false)}
                  className="flex items-center justify-center p-2 text-slate-400 hover:text-white hover:bg-sidebar-700 rounded-lg transition-colors"
                  title="Colapsar menú"
                >
                  <ChevronLeft className="w-4 h-4" />
                </button>
              </div>
            </div>
          ) : (
            <div className="p-3 flex flex-col items-center gap-2">
              <button
                onClick={handleLogout}
                className="p-2 text-slate-400 hover:text-danger-400 hover:bg-danger-500/10 rounded-lg transition-colors"
                title="Cerrar sesión"
              >
                <LogOut className="w-5 h-5" />
              </button>
              <button
                onClick={() => setSidebarOpen(true)}
                className="p-2 text-slate-400 hover:text-white hover:bg-sidebar-700 rounded-lg transition-colors"
                title="Expandir menú"
              >
                <ChevronRight className="w-5 h-5" />
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
        <header className="sticky top-0 z-30 bg-white border-b border-slate-200 shadow-sm">
          <div className="flex items-center justify-between h-16 lg:h-20 px-3 lg:px-6">
            {/* Left: Breadcrumb */}
            <div className="flex items-center gap-2 min-w-0 flex-1">
              <button
                onClick={() => setMobileOpen(true)}
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
              <button className="relative p-2 rounded-lg hover:bg-slate-100 text-slate-500 transition-colors flex-shrink-0">
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
        <div className="p-3 lg:p-6">
          {children}
        </div>
      </main>
    </div>
  );
}
