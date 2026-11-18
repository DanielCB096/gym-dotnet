import { Loader2 } from 'lucide-react';

const Table = ({ columns = [], data = [], loading = false, emptyText = 'No hay datos disponibles', className = '' }) => {
  return (
    <div className={`rounded-xl border border-slate-200 overflow-hidden bg-white shadow-sm ${className}`}>
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead>
            <tr className="bg-slate-50 border-b border-slate-200">
              {columns.map((col, i) => (
                <th
                  key={i}
                  className="px-6 py-3.5 text-left text-xs font-medium text-slate-500 uppercase tracking-wider"
                >
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {loading ? (
              <tr>
                <td colSpan={columns.length} className="px-6 py-12 text-center">
                  <div className="flex flex-col items-center gap-3 text-slate-400">
                    <Loader2 className="w-6 h-6 animate-spin" />
                    <span className="text-sm">Cargando datos...</span>
                  </div>
                </td>
              </tr>
            ) : data.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="px-6 py-12 text-center text-sm text-slate-400">
                  {emptyText}
                </td>
              </tr>
            ) : (
              data.map((row, rowIndex) => (
                <tr key={rowIndex} className="hover:bg-slate-50 transition-colors">
                  {columns.map((col, colIndex) => (
                    <td key={colIndex} className="px-6 py-4 text-sm text-slate-600">
                      {col.render ? col.render(row[col.accessor], row) : row[col.accessor]}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default Table;
