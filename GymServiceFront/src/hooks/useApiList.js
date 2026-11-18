import { useCallback, useEffect, useRef, useState } from 'react';
import api from '../services/api';

/**
 * Hook para listar datos paginados de la API.
 * Extrae `data`/`items` y `pagination` del sobre estándar del backend.
 */
export default function useApiList(endpoint, { limit = 50, params = {}, enabled = true } = {}) {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(enabled);
  const [error, setError] = useState('');
  const [pagination, setPagination] = useState(null);
  const [page, setPage] = useState(1);
  const requestId = useRef(0);
  const paramsKey = JSON.stringify(params);

  const load = useCallback(async () => {
    if (!enabled) return;
    const id = ++requestId.current;
    setLoading(true);
    setError('');

    try {
      const response = await api.get(endpoint, {
        params: { page, limit, ...JSON.parse(paramsKey) },
      });
      if (id !== requestId.current) return;

      const body = response.data || {};
      const data = body.data ?? body.items ?? (Array.isArray(body) ? body : []);
      setItems(Array.isArray(data) ? data : []);
      setPagination(body.pagination ?? null);
    } catch (err) {
      if (id !== requestId.current) return;
      setItems([]);
      setPagination(null);
      setError(err.response?.data?.message || 'No se pudieron cargar los datos.');
    } finally {
      if (id === requestId.current) setLoading(false);
    }
  }, [endpoint, page, limit, paramsKey, enabled]);

  useEffect(() => {
    let active = true;
    Promise.resolve().then(() => {
      if (active) load();
    });
    return () => {
      active = false;
    };
  }, [load]);

  return {
    items,
    setItems,
    loading,
    error,
    pagination,
    page,
    setPage,
    limit,
    reload: load,
  };
}
