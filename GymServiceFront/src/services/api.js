import axios from 'axios';

// Usar proxy de Vite en desarrollo (rutas relativas)
const API_BASE = '/api';

const api = axios.create({
  baseURL: API_BASE,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Agregar token a cada petición + log de depuración
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  console.log('[API] Petición:', config.method?.toUpperCase(), config.url);
  return config;
});

// Manejar errores 401 (token expirado) - solo si NO es la ruta de login
api.interceptors.response.use(
  (response) => {
    console.log('[API] Respuesta:', response.status, response.config.url);
    return response;
  },
  (error) => {
    console.error('[API] Error:', error.response?.status, error.config?.url, error.message);
    
    // No interceptar errores de login para que el componente los maneje
    const isLoginRequest = error.config?.url?.includes('/auth/login');
    
    if (error.response && error.response.status === 401 && !isLoginRequest) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default api;
