import { useState } from 'react';
import { AlertCircle, Boxes, Upload } from 'lucide-react';
import api from '../services/api';
import CrudPage from '../components/CrudPage';
import ImportDialog from '../components/ImportDialog';
import Badge from '../components/ui/Badge';
import Modal from '../components/ui/Modal';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Select from '../components/ui/Select';
import { formatCurrency } from '../utils/format';

export default function Inventory() {
  const [stockTarget, setStockTarget] = useState(null);
  const [stockValue, setStockValue] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [importTarget, setImportTarget] = useState(null);

  const openStock = (product, reload) => {
    setStockTarget({ product, reload });
    setStockValue(String(product.stock ?? 0));
    setError('');
  };

  const closeStock = () => {
    if (saving) return;
    setStockTarget(null);
    setError('');
  };

  const saveStock = async (event) => {
    event.preventDefault();
    if (!stockTarget) return;

    const newStock = Number(stockValue);
    if (!Number.isInteger(newStock) || newStock < 0) {
      setError('El stock debe ser un número entero mayor o igual a cero.');
      return;
    }

    setSaving(true);
    setError('');
    try {
      const response = await api.put(`/inventory/${stockTarget.product.id}/stock`, { stock: newStock });
      stockTarget.reload();
      setStockTarget(null);
      return response.data?.message;
    } catch (err) {
      setError(err.response?.data?.message || 'No se pudo actualizar el stock.');
    } finally {
      setSaving(false);
    }
  };

  const createFields = [
    { name: 'productName', label: 'Nombre del producto', required: true },
    { name: 'sku', label: 'SKU', placeholder: 'Código interno (opcional)' },
    { name: 'category', label: 'Categoría', placeholder: 'Ej. Suplementos' },
    { name: 'description', label: 'Descripción', type: 'textarea', full: true, rows: 2 },
    { name: 'price', label: 'Precio de venta', type: 'number', min: 0, step: '0.01', defaultValue: 0 },
    { name: 'cost', label: 'Costo', type: 'number', min: 0, step: '0.01', placeholder: 'Opcional' },
    { name: 'stock', label: 'Stock inicial', type: 'number', min: 0, defaultValue: 0 },
    { name: 'minStock', label: 'Stock mínimo', type: 'number', min: 0, defaultValue: 5 },
  ];

  return (
    <>
      <CrudPage
        title="Inventario"
        subtitle="Productos disponibles para la venta en recepción"
        endpoint="/inventory"
        extraParams={{ includeInactive: true, lowStock: lowStockOnly || undefined }}
        createLabel="Nuevo producto"
        emptyText="No hay productos registrados"
        searchParam="query"
        searchPlaceholder="Buscar por nombre, categoría o SKU"
        toolbar={({ reload }) => (
          <div className="flex flex-wrap items-end gap-2">
            <Select
              label="Existencias"
              value={lowStockOnly ? 'low' : ''}
              onChange={(event) => setLowStockOnly(event.target.value === 'low')}
              options={[{ value: 'low', label: 'Solo stock bajo' }]}
              placeholder="Todos"
            />
            <Button
              variant="outline"
              icon={Upload}
              onClick={() => setImportTarget({ reload })}
            >
              Importar
            </Button>
            <Button variant="outline" onClick={reload}>Actualizar</Button>
          </div>
        )}
        columns={[
          {
            key: 'productName',
            label: 'Producto',
            render: (row) => (
              <div>
                <p className="font-medium text-slate-800">{row.productName}</p>
                <p className="text-xs text-slate-400">{row.sku || row.category || '—'}</p>
              </div>
            ),
          },
          { key: 'category', label: 'Categoría', render: (row) => row.category || '—' },
          { key: 'price', label: 'Precio', render: (row) => formatCurrency(row.price) },
          {
            key: 'cost',
            label: 'Costo',
            render: (row) => (row.cost != null ? formatCurrency(row.cost) : '—'),
          },
          {
            key: 'stock',
            label: 'Stock',
            render: (row) => {
              const low = Number(row.stock) <= Number(row.minStock);
              return (
                <Badge variant={low ? 'warning' : 'success'} dot>
                  {`${row.stock ?? 0}${low ? ' · bajo' : ''}`}
                </Badge>
              );
            },
          },
          { key: 'minStock', label: 'Mínimo' },
          {
            key: 'isActive',
            label: 'Estado',
            render: (row) => (
              <Badge variant={row.isActive ? 'success' : 'neutral'} dot>
                {row.isActive ? 'Activo' : 'Inactivo'}
              </Badge>
            ),
          },
        ]}
        fields={createFields}
        createFields={createFields}
        editFields={[
          ...createFields.filter((field) => field.name !== 'stock'),
          {
            name: 'isActive',
            label: 'Estado',
            type: 'select',
            options: [
              { value: 'true', label: 'Activo' },
              { value: 'false', label: 'Inactivo' },
            ],
          },
        ]}
        mapItemToForm={(item, initial) => ({
          ...initial,
          isActive: item.isActive ? 'true' : 'false',
        })}
        actions={[
          {
            label: 'Ajustar stock',
            icon: Boxes,
            onClick: (item, { reload }) => openStock(item, reload),
          },
        ]}
      />

      <Modal
        isOpen={!!stockTarget}
        onClose={closeStock}
        title={`Ajustar stock · ${stockTarget?.product?.productName || ''}`}
        size="sm"
      >
        <form onSubmit={saveStock} className="space-y-4">
          <p className="text-sm text-slate-500">
            Stock actual: <span className="font-medium text-slate-700">{stockTarget?.product?.stock ?? 0}</span>
          </p>
          <Input
            id="stockValue"
            label="Nuevo stock"
            type="number"
            min="0"
            step="1"
            value={stockValue}
            onChange={(event) => setStockValue(event.target.value)}
            required
          />

          {error && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={closeStock} disabled={saving}>
              Cancelar
            </Button>
            <Button type="submit" loading={saving}>
              Guardar
            </Button>
          </div>
        </form>
      </Modal>

      {importTarget && (
        <ImportDialog
          entity="inventory"
          isOpen
          onClose={() => setImportTarget(null)}
          onImported={() => importTarget.reload()}
        />
      )}
    </>
  );
}
