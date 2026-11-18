import { useState, useEffect } from 'react';
import api from '../services/api';

export default function Sales() {
  const [sales, setSales] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get('/sales')
      .then((r) => setSales(r.data?.data || r.data?.items || []))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <svg className="animate-spin w-8 h-8 mx-auto text-brand-600 mb-3" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <p className="text-slate-500">Cargando ventas...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="animate-fade-in">
      {/* Page header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
        <div>
          <h2 className="text-2xl lg:text-3xl font-bold text-slate-800 tracking-tight">Ventas</h2>
          <p className="text-slate-500 mt-1">{sales.length} venta{sales.length !== 1 ? 's' : ''} registrada{sales.length !== 1 ? 's' : ''}</p>
        </div>
      </div>

      {/* Table card */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        {sales.length === 0 ? (
          <div className="px-6 py-16 text-center">
            <div className="w-16 h-16 mx-auto mb-4 rounded-2xl bg-slate-50 flex items-center justify-center">
              <svg className="w-8 h-8 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M12 1v22M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6" />
              </svg>
            </div>
            <p className="text-slate-500 font-medium">No hay ventas registradas</p>
            <p className="text-slate-400 text-sm mt-1">Las ventas aparecerán aquí cuando se realicen</p>
          </div>
        ) : (
          <div className="table-responsive">
            <table className="w-full">
              <thead>
                <tr className="bg-slate-50 border-b border-slate-100">
                  <th className="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">No. Venta</th>
                  <th className="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Miembro</th>
                  <th className="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Total</th>
                  <th className="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider hidden sm:table-cell">Método</th>
                  <th className="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Fecha</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50">
                {sales.map((s, i) => (
                  <tr key={i} className="hover:bg-slate-50/50 transition-colors">
                    <td className="px-5 py-4">
                      <span className="inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-medium bg-brand-50 text-brand-700">
                        {s.saleNumber || '—'}
                      </span>
                    </td>
                    <td className="px-5 py-4 text-sm text-slate-700">{s.memberName || <span className="text-slate-300">—</span>}</td>
                    <td className="px-5 py-4">
                      <span className="text-sm font-semibold text-emerald-600">
                        ${s.totalAmount?.toFixed(2) || '0.00'}
                      </span>
                    </td>
                    <td className="px-5 py-4 text-sm text-slate-600 hidden sm:table-cell">{s.paymentMethod || <span className="text-slate-300">—</span>}</td>
                    <td className="px-5 py-4 text-sm text-slate-600">{s.saleDate || <span className="text-slate-300">—</span>}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
