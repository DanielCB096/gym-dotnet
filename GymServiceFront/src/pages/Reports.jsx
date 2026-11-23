import { useState } from 'react';
import { Download, Loader2 } from 'lucide-react';
import api from '../services/api';
import CrudPage from '../components/CrudPage';
import Button from '../components/ui/Button';
import { formatDateTime } from '../utils/format';

const exports = [
  { label: 'Ventas', url: '/reports/sales/excel', filename: 'ventas.xlsx' },
  { label: 'Miembros', url: '/reports/members/excel', filename: 'miembros.xlsx' },
  { label: 'Asistencia', url: '/reports/attendance/excel', filename: 'asistencia.xlsx' },
  { label: 'Inventario', url: '/reports/inventory/excel', filename: 'inventario.xlsx' },
  { label: 'Caja', url: '/reports/cash/excel', filename: 'caja.xlsx' },
  { label: 'Membresías', url: '/reports/memberships/excel', filename: 'membresias.xlsx' },
  { label: 'Kardex', url: '/reports/kardex/excel', filename: 'kardex.xlsx' },
];

function ExportButtons({ notify }) {
  const [busy, setBusy] = useState('');

  const download = async (item) => {
    setBusy(item.url);
    try {
      const response = await api.get(item.url, { responseType: 'blob' });
      const disposition = response.headers['content-disposition'] || '';
      const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
      const filename = match ? decodeURIComponent(match[1]) : item.filename;

      const url = URL.createObjectURL(response.data);
      const link = document.createElement('a');
      link.href = url;
      link.download = filename;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
    } catch (err) {
      notify('error', err.response?.data?.message || 'No se pudo descargar el reporte.');
    } finally {
      setBusy('');
    }
  };

  return (
    <div className="flex flex-wrap items-center gap-2">
      <span className="text-sm text-slate-500 mr-1">Exportar a Excel:</span>
      {exports.map((item) => (
        <Button
          key={item.url}
          variant="outline"
          size="sm"
          onClick={() => download(item)}
          disabled={!!busy}
          icon={busy === item.url ? Loader2 : Download}
        >
          {item.label}
        </Button>
      ))}
    </div>
  );
}

export default function Reports() {
  return (
    <CrudPage
      title="Reportes programados"
      subtitle="Generación automática de reportes y exportación a Excel"
      endpoint="/reports/scheduled"
      createLabel="Programar reporte"
      emptyText="No hay reportes programados"
      canEdit={false}
      columns={[
        { key: 'reportName', label: 'Reporte' },
        { key: 'reportType', label: 'Tipo' },
        { key: 'schedule', label: 'Programación' },
        { key: 'emailRecipients', label: 'Destinatarios' },
        { key: 'lastRun', label: 'Última ejecución', render: (row) => formatDateTime(row.lastRun) },
        { key: 'nextRun', label: 'Próxima ejecución', render: (row) => formatDateTime(row.nextRun) },
      ]}
      fields={[
        { name: 'reportName', label: 'Nombre del reporte', required: true },
        {
          name: 'reportType',
          label: 'Tipo de reporte',
          type: 'select',
          defaultValue: 'sales',
          options: [
            { value: 'sales', label: 'Ventas' },
            { value: 'members', label: 'Miembros' },
            { value: 'attendance', label: 'Asistencia' },
            { value: 'inventory', label: 'Inventario' },
            { value: 'cash', label: 'Caja' },
            { value: 'memberships', label: 'Membresías' },
            { value: 'kardex', label: 'Kardex' },
          ],
        },
        {
          name: 'schedule',
          label: 'Frecuencia',
          type: 'select',
          defaultValue: 'weekly',
          options: [
            { value: 'daily', label: 'Diario' },
            { value: 'weekly', label: 'Semanal' },
            { value: 'monthly', label: 'Mensual' },
          ],
        },
        {
          name: 'emailRecipients',
          label: 'Destinatarios',
          required: true,
          full: true,
          placeholder: 'dueno@centrofit.mx, administracion@centrofit.mx',
          help: 'Separa varios correos con coma. Se envía un Excel adjunto.',
        },
      ]}
      toolbar={({ notify }) => <ExportButtons notify={notify} />}
    />
  );
}
