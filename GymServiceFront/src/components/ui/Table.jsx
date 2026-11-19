import { Inbox, Loader2 } from 'lucide-react';

const Table = ({
  columns = [],
  data = [],
  loading = false,
  emptyText = 'No hay datos disponibles',
  emptyAction = null,
  emptyIcon: EmptyIcon = Inbox,
  className = '',
}) => {
  const cardClass = `overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm ${className}`;

  // Sin datos (o cargando) se muestra un estado a ancho completo, sin tabla:
  // así el mensaje y el botón no quedan fuera de pantalla en móvil.
  if (loading || data.length === 0) {
    return (
      <div className={cardClass}>
        <div className="flex flex-col items-center gap-4 px-6 py-16 text-center">
          {loading ? (
            <>
              <Loader2 className="h-6 w-6 animate-spin text-slate-400" />
              <p className="text-sm text-slate-500">Cargando datos...</p>
            </>
          ) : (
            <>
              <div className="flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-400">
                <EmptyIcon className="h-6 w-6" />
              </div>
              <div>
                <p className="text-sm font-medium text-slate-700">{emptyText}</p>
                <p className="mt-1 text-sm text-slate-400">
                  Aún no hay información para mostrar.
                </p>
              </div>
              {emptyAction}
            </>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className={cardClass}>
      <div className="overflow-x-auto">
        <table className="w-full min-w-[640px] border-collapse">
          <thead>
            <tr className="border-b border-slate-200 bg-slate-50/80">
              {columns.map((col, i) => (
                <th
                  key={i}
                  className="whitespace-nowrap px-6 py-3 text-left text-[11px] font-semibold uppercase tracking-wider text-slate-500"
                >
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {data.map((row, rowIndex) => (
              <tr
                key={rowIndex}
                className="transition-colors hover:bg-slate-50/70"
              >
                {columns.map((col, colIndex) => (
                  <td
                    key={colIndex}
                    className="px-6 py-3.5 text-sm text-slate-600"
                  >
                    {col.render ? col.render(row[col.accessor], row) : row[col.accessor]}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default Table;
