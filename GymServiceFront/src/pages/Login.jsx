import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { Dumbbell, User, Lock, Eye, EyeOff } from 'lucide-react';

export default function Login() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      await login(username, password);
      navigate('/');
    } catch (err) {
      console.error('Login error:', err);
      
      if (err.response) {
        const status = err.response.status;
        const message = err.response.data?.message;
        
        if (status === 401) {
          setError('Usuario o contraseña incorrectos. Inténtalo de nuevo.');
        } else if (status === 403) {
          setError('Cuenta deshabilitada. Contacta al administrador.');
        } else if (status >= 500) {
          setError('Error del servidor. Inténtalo de nuevo más tarde.');
        } else {
          setError(message || 'Error al iniciar sesión. Verifica tus credenciales.');
        }
      } 
      else if (err.request) {
        setError('No se puede conectar al servidor. Verifica que el backend esté corriendo.');
      }
      else {
        setError('Error inesperado: ' + (err.message || 'Verifica tus credenciales.'));
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex animate-fade-in">
      {/* Left side - Branding (hidden on mobile) */}
      <div className="hidden lg:flex lg:w-1/2 bg-gradient-to-br from-brand-600 via-brand-700 to-brand-900 relative overflow-hidden">
        {/* Decorative circles */}
        <div className="absolute top-0 right-0 w-96 h-96 bg-white/5 rounded-full -translate-y-1/2 translate-x-1/2"></div>
        <div className="absolute bottom-0 left-0 w-64 h-64 bg-white/5 rounded-full translate-y-1/2 -translate-x-1/2"></div>
        <div className="absolute top-1/2 left-1/2 w-32 h-32 bg-white/5 rounded-full -translate-x-1/2 -translate-y-1/2"></div>
        
        <div className="relative z-10 flex flex-col items-center justify-center w-full p-8 lg:p-12 text-white">
          <div className="w-16 h-16 lg:w-20 lg:h-20 bg-white/10 backdrop-blur-sm rounded-2xl flex items-center justify-center mb-6 lg:mb-8 shadow-2xl">
            <Dumbbell className="w-8 h-8 lg:w-10 lg:h-10" />
          </div>
          <h1 className="text-3xl lg:text-5xl font-bold mb-3 lg:mb-4 tracking-tight">GymService</h1>
          <p className="text-base lg:text-xl text-brand-200 text-center max-w-sm lg:max-w-md leading-relaxed px-4">
            Sistema integral de gestión para tu gimnasio. Controla miembros, ventas, inventario y más.
          </p>
          
          {/* Feature list */}
          <div className="mt-8 lg:mt-12 space-y-3 lg:space-y-4 px-4">
            {[
              { icon: '👥', text: 'Gestión de miembros' },
              { icon: '💰', text: 'Control de ventas' },
              { icon: '📊', text: 'Reportes en tiempo real' },
              { icon: '🔒', text: 'Seguridad avanzada' },
            ].map((feature, i) => (
              <div key={i} className="flex items-center gap-3 text-brand-100">
                <span className="text-base lg:text-lg">{feature.icon}</span>
                <span className="text-sm">{feature.text}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Right side - Login form */}
      <div className="w-full lg:w-1/2 flex items-center justify-center p-4 sm:p-6 lg:p-8 bg-white">
        <div className="w-full max-w-sm">
          {/* Mobile logo */}
          <div className="lg:hidden flex items-center justify-center gap-3 mb-6 lg:mb-8">
            <div className="w-12 h-12 bg-gradient-to-br from-brand-500 to-brand-700 rounded-xl flex items-center justify-center shadow-lg">
              <Dumbbell className="w-6 h-6 text-white" />
            </div>
            <span className="text-2xl font-bold text-slate-800 tracking-tight">GymService</span>
          </div>

          <div className="mb-6 lg:mb-8">
            <h2 className="text-2xl font-bold text-slate-800 mb-1">Bienvenido</h2>
            <p className="text-sm text-slate-500">Inicia sesión para acceder al panel</p>
          </div>

          {/* Error message */}
          {error && (
            <div className="mb-4 lg:mb-6 p-3 lg:p-4 bg-danger-50 border border-danger-200 rounded-lg flex items-start gap-3 animate-fade-in">
              <svg className="w-5 h-5 text-danger-500 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p className="text-xs lg:text-sm text-danger-700">{error}</p>
            </div>
          )}

          {/* Form */}
          <form onSubmit={handleSubmit} className="space-y-4 lg:space-y-5">
            <div>
              <label htmlFor="username" className="block text-sm font-medium text-slate-700 mb-1.5">
                Usuario
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                  <User className="w-4 h-4" />
                </div>
                <input
                  id="username"
                  type="text"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  className="w-full pl-10 pr-4 py-2.5 lg:py-3.5 border border-slate-300 rounded-lg focus:ring-2 focus:ring-brand-500/50 focus:border-brand-500 outline-none transition-all bg-slate-50 focus:bg-white text-sm text-slate-800 placeholder-slate-400"
                  placeholder="Ingresa tu usuario"
                  required
                  autoComplete="username"
                />
              </div>
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium text-slate-700 mb-1.5">
                Contraseña
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
                  <Lock className="w-4 h-4" />
                </div>
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="w-full pl-10 pr-12 py-2.5 lg:py-3.5 border border-slate-300 rounded-lg focus:ring-2 focus:ring-brand-500/50 focus:border-brand-500 outline-none transition-all bg-slate-50 focus:bg-white text-sm text-slate-800 placeholder-slate-400"
                  placeholder="Ingresa tu contraseña"
                  required
                  autoComplete="current-password"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-slate-400 hover:text-slate-600 transition-colors"
                >
                  {showPassword ? (
                    <EyeOff className="w-4 h-4" />
                  ) : (
                    <Eye className="w-4 h-4" />
                  )}
                </button>
              </div>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full bg-brand-500 hover:bg-brand-600 text-white font-semibold py-2.5 lg:py-3.5 rounded-lg transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed shadow-sm hover:shadow-md active:scale-[0.98]"
            >
              {loading ? (
                <span className="flex items-center justify-center gap-2">
                  <svg className="animate-spin w-5 h-5" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  Iniciando sesión...
                </span>
              ) : (
                'Iniciar sesión'
              )}
            </button>
          </form>

          {/* Footer */}
          <p className="text-center text-slate-400 text-xs mt-6 lg:mt-8">
            © 2026 GymService · Todos los derechos reservados
          </p>
        </div>
      </div>
    </div>
  );
}
