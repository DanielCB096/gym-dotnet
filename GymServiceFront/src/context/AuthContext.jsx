import { createContext, useState, useContext, useEffect } from 'react';
import api from '../services/api';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    try {
      const token = localStorage.getItem('token');
      const storedUser = localStorage.getItem('user');
      if (token && storedUser) {
        const parsed = JSON.parse(storedUser);
        if (parsed && typeof parsed === 'object') {
          setUser(parsed);
        } else {
          // Datos corruptos, limpiar
          localStorage.removeItem('token');
          localStorage.removeItem('user');
        }
      }
    } catch {
      // JSON.parse falló: datos corruptos, limpiar
      console.warn('AuthContext: datos de usuario corruptos, limpiando localStorage');
      localStorage.removeItem('token');
      localStorage.removeItem('user');
    }
    setLoading(false);
  }, []);

  const persistSession = (data) => {
    if (!data || !data.token) {
      throw new Error('Respuesta de autenticación inválida');
    }

    // El backend devuelve: { token, userId, role, username, permissions, expiresIn }
    const userData = {
      userId: data.userId,
      role: data.role,
      username: data.username,
      fullName: data.fullName || data.username,
      permissions: data.permissions || []
    };
    localStorage.setItem('token', data.token);
    localStorage.setItem('user', JSON.stringify(userData));
    setUser(userData);
    return userData;
  };

  const login = async (username, password) => {
    const response = await api.post('/auth/login', { username, password });
    const data = response.data;

    // La contraseña es correcta pero falta el segundo factor.
    if (data?.requiresMfa) {
      return {
        requiresMfa: true,
        mfaToken: data.mfaToken,
        expiresIn: data.expiresIn
      };
    }

    return persistSession(data);
  };

  const verifyMfaLogin = async (mfaToken, code) => {
    const response = await api.post('/auth/mfa/login', { mfaToken, code });
    return persistSession(response.data);
  };

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setUser(null);
  };

  const isAuthenticated = !!user;

  return (
    <AuthContext.Provider value={{ user, login, verifyMfaLogin, logout, isAuthenticated, loading }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  return useContext(AuthContext);
}
