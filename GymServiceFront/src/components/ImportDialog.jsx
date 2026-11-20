import { useRef, useState } from 'react';
import { AlertCircle, CheckCircle2, Download, Upload } from 'lucide-react';
import api from '../services/api';
import Modal from './ui/Modal';
import Button from './ui/Button';
import Select from './ui/Select';

const ENTITIES = {
  members: {
    title: 'Importar socios',
    description:
      'Carga un archivo CSV o Excel con los datos de los socios. Se crean como cuentas activas, sin membresía.',
    columns: [
      'FirstName',
      'LastName',
      'Email',
      'Phone',
      'CURP',
      'RFC',
      'Address',
      'EmergencyContact',
      'EmergencyPhone',
    ],
  },
  inventory: {
    title: 'Importar productos',
    description:
      'Carga un archivo CSV o Excel con los productos del inventario. Los productos se crean activos.',
    columns: [
      'ProductName',
      'Description',
      'SKU',
      'Category',
      'Price',
      'Cost',
      'StockQuantity',
      'MinStock',
    ],
  },
};

const FORMATS = [
  { value: 'csv', label: 'CSV (.csv)' },
  { value: 'excel', label: 'Excel (.xlsx)' },
];

const ENCODINGS = [
  { value: 'utf-8', label: 'UTF-8 (recomendado)' },
  { value: 'latin1', label: 'Latin-1 (CSV guardado desde Excel)' },
];

const filenameFromDisposition = (disposition, fallback) => {
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition || '');
  return match ? decodeURIComponent(match[1]) : fallback;
};

const saveBlob = (blob, filename) => {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
};

/**
 * Importación masiva desde CSV/Excel para socios o inventario.
 * Usa los endpoints /api/import/* (CsvHelper y ClosedXML en el backend).
 */
export default function ImportDialog({ entity, isOpen, onClose, onImported }) {
  const config = ENTITIES[entity];
  const fileRef = useRef(null);
  const [format, setFormat] = useState('csv');
  const [encoding, setEncoding] = useState('utf-8');
  const [file, setFile] = useState(null);
  const [busy, setBusy] = useState(false);
  const [downloading, setDownloading] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState('');

  if (!config) return null;

  const close = () => {
    if (busy) return;
    onClose();
  };

  const downloadTemplate = async () => {
    setDownloading(true);
    setError('');
    try {
      const response = await api.get(`/import/templates/${entity}/${format}`, {
        responseType: 'blob',
      });
      const fallback = `plantilla_${entity}.${format === 'csv' ? 'csv' : 'xlsx'}`;
      saveBlob(response.data, filenameFromDisposition(response.headers['content-disposition'], fallback));
    } catch {
      setError('No se pudo descargar la plantilla. Revisa tu sesión y permisos.');
    } finally {
      setDownloading(false);
    }
  };

  const applyResult = (payload) => {
    const errors = Array.isArray(payload.errors) ? payload.errors : [];
    setResult({ ...payload, errors });
    if (Number(payload.importedRows) > 0) {
      onImported?.();
    }
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!file) {
      setError('Selecciona un archivo CSV o Excel.');
      return;
    }

    setBusy(true);
    setError('');
    setResult(null);

    try {
      const data = new FormData();
      data.append('file', file);
      const params = format === 'csv' ? { encoding } : undefined;
      const response = await api.post(`/import/${entity}/${format}`, data, { params });
      applyResult(response.data || {});
    } catch (err) {
      const payload = err.response?.data;
      if (payload && typeof payload === 'object' && !(payload instanceof Blob)) {
        // La importación puede ser parcial: el backend devuelve 400 con el detalle.
        applyResult(payload);
      } else if (err.response?.status === 403) {
        setError('No tienes permiso para importar este tipo de datos.');
      } else {
        setError('No se pudo completar la importación. Revisa el archivo e inténtalo de nuevo.');
      }
    } finally {
      setBusy(false);
    }
  };

  const counts = [
    { label: 'Filas leídas', value: result?.totalRows ?? 0, tone: 'text-slate-700' },
    { label: 'Importadas', value: result?.importedRows ?? 0, tone: 'text-success-600' },
    { label: 'Errores', value: result?.errorRows ?? 0, tone: 'text-danger-600' },
    { label: 'Omitidas', value: result?.skippedRows ?? 0, tone: 'text-warning-600' },
  ];

  return (
    <Modal isOpen={isOpen} onClose={close} title={config.title} size="lg">
      <form onSubmit={handleSubmit} className="space-y-4">
        <p className="text-sm text-slate-500">{config.description}</p>

        <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3">
          <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-500">
            Columnas esperadas
          </p>
          <code className="mt-1 block break-words text-xs text-slate-600">
            {config.columns.join(', ')}
          </code>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            icon={Download}
            loading={downloading}
            onClick={downloadTemplate}
            className="mt-2"
          >
            Descargar plantilla {format === 'csv' ? 'CSV' : 'Excel'}
          </Button>
        </div>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <Select
            id="import-format"
            label="Formato"
            value={format}
            onChange={(event) => {
              setFormat(event.target.value);
              setFile(null);
              setResult(null);
              setError('');
              if (fileRef.current) fileRef.current.value = '';
            }}
            options={FORMATS}
            disabled={busy}
          />
          {format === 'csv' && (
            <Select
              id="import-encoding"
              label="Codificación"
              value={encoding}
              onChange={(event) => setEncoding(event.target.value)}
              options={ENCODINGS}
              disabled={busy}
            />
          )}
        </div>

        <div>
          <label htmlFor="import-file" className="mb-1.5 block text-sm font-medium text-slate-700">
            Archivo
          </label>
          <input
            ref={fileRef}
            id="import-file"
            type="file"
            accept={
              format === 'csv'
                ? '.csv,text/csv'
                : '.xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
            }
            disabled={busy}
            onChange={(event) => {
              setFile(event.target.files?.[0] || null);
              setResult(null);
              setError('');
            }}
            className="block w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-600 file:mr-3 file:rounded-lg file:border-0 file:bg-brand-50 file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-brand-600 hover:file:bg-brand-100 disabled:opacity-50"
          />
          {file && (
            <p className="mt-1 text-xs text-slate-400">
              {file.name} · {(file.size / 1024).toFixed(1)} KB
            </p>
          )}
        </div>

        {error && (
          <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
            <AlertCircle className="mt-0.5 h-4 w-4 flex-shrink-0" />
            <span>{error}</span>
          </div>
        )}

        {result && (
          <div
            className={`rounded-xl border px-4 py-3 text-sm ${
              result.success
                ? 'border-success-200 bg-success-50 text-success-700'
                : 'border-warning-200 bg-warning-50 text-warning-700'
            }`}
            role="status"
          >
            <div className="flex items-start gap-2">
              {result.success ? (
                <CheckCircle2 className="mt-0.5 h-4 w-4 flex-shrink-0" />
              ) : (
                <AlertCircle className="mt-0.5 h-4 w-4 flex-shrink-0" />
              )}
              <span>{result.message || 'Importación procesada.'}</span>
            </div>

            <div className="mt-3 grid grid-cols-2 gap-2 sm:grid-cols-4">
              {counts.map((item) => (
                <div key={item.label} className="rounded-lg bg-white/70 px-3 py-2 text-center">
                  <p className={`text-lg font-semibold ${item.tone}`}>{item.value}</p>
                  <p className="text-[11px] text-slate-500">{item.label}</p>
                </div>
              ))}
            </div>

            {result.errors.length > 0 && (
              <div className="mt-3 max-h-48 overflow-y-auto rounded-lg border border-slate-200 bg-white">
                <table className="w-full text-left text-xs">
                  <thead className="sticky top-0 bg-slate-50 text-slate-500">
                    <tr>
                      <th className="px-3 py-2 font-semibold">Fila</th>
                      <th className="px-3 py-2 font-semibold">Datos</th>
                      <th className="px-3 py-2 font-semibold">Error</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100 text-slate-600">
                    {result.errors.map((item, index) => (
                      <tr key={`${item.rowNumber}-${index}`}>
                        <td className="whitespace-nowrap px-3 py-2">{item.rowNumber}</td>
                        <td className="px-3 py-2">{item.rowData || '—'}</td>
                        <td className="px-3 py-2 text-danger-600">{item.errorMessage}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}

        <div className="flex justify-end gap-2 pt-2">
          <Button variant="ghost" onClick={close} disabled={busy}>
            {result ? 'Cerrar' : 'Cancelar'}
          </Button>
          <Button type="submit" icon={Upload} loading={busy} disabled={!file}>
            Importar
          </Button>
        </div>
      </form>
    </Modal>
  );
}
