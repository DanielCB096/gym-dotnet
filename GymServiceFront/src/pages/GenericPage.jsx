import { useState, useEffect } from 'react';
import { Loader2 } from 'lucide-react';
import api from '../services/api';
import Table from '../components/ui/Table';

// Generic data table page
export default function GenericPage({ title, endpoint, columns, emptyMsg }) {
  const [data, setData] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get(endpoint)
      .then((r) => {
        const items = r.data?.data || r.data?.items || r.data || [];
        setData(Array.isArray(items) ? items : []);
      })
      .catch((err) => {
        console.error(`[GenericPage] Error cargando ${endpoint}:`, err);
      })
      .finally(() => setLoading(false));
  }, [endpoint]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <Loader2 className="w-8 h-8 mx-auto text-brand-500 animate-spin mb-3" />
          <p className="text-sm text-slate-500">Cargando {title.toLowerCase()}...</p>
        </div>
      </div>
    );
  }

  const items = Array.isArray(data) ? data : [];

  // Map columns to Table component format
  const tableColumns = columns.map(col => ({
    header: col.label,
    accessor: col.key,
    render: col.render ? (value, row) => col.render(row) : undefined,
  }));

  return (
    <div className="animate-fade-in">
      {/* Page header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-4 lg:mb-6">
        <div className="min-w-0">
          <h2 className="text-xl lg:text-2xl font-bold text-slate-800 tracking-tight truncate">{title}</h2>
          <p className="text-sm text-slate-500 mt-0.5">
            {items.length} registro{items.length !== 1 ? 's' : ''} encontrado{items.length !== 1 ? 's' : ''}
          </p>
        </div>
      </div>

      {/* Table */}
      <Table
        columns={tableColumns}
        data={items}
        loading={false}
        emptyText={emptyMsg || `No hay registros de ${title.toLowerCase()}`}
      />
    </div>
  );
}
