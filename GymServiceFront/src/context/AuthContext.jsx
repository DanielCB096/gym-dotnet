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
    } catch (err) {
      // JSON.parse falló: datos corruptos, limpiar
      console.warn('AuthContext: datos de usuario corruptos, limpiando localStorage');
      localStorage.removeItem('token');
      localStorage.removeItem('user');
    }
    setLoading(false);
  }, []);

  const login = async (username, password) => {
    const response = await api.post('/auth/login', { username, password });
    const data = response.data;
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

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setUser(null);
  };

  const isAuthenticated = !!user;

  return (
    <AuthContext.Provider value={{ user, login, logout, isAuthenticated, loading }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  return useContext(AuthContext);
}
