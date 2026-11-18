import { useState } from 'react';
import { AlertCircle, Boxes } from 'lucide-react';
import api from '../services/api';
import CrudPage from '../components/CrudPage';
import Badge from '../components/ui/Badge';
import Modal from '../components/ui/Modal';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import { formatCurrency } from '../utils/format';

export default function Inventory() {
  const [stockTarget, setStockTarget] = useState(null);
  const [stockValue, setStockValue] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

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
      await api.put(`/inventory/${stockTarget.product.id}/stock`, { stock: newStock });
      stockTarget.reload();
      setStockTarget(null);
    } catch (err) {
      setError(err.response?.data?.message || 'No se pudo actualizar el stock.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <CrudPage
        title="Inventario"
        subtitle="Productos disponibles para la venta en recepción"
        endpoint="/inventory"
        createLabel="Nuevo producto"
        emptyText="No hay productos registrados"
        searchParam="query"
        searchPlaceholder="Buscar por nombre o categoría"
        canEdit={false}
        columns={[
          { key: 'productName', label: 'Producto' },
          { key: 'category', label: 'Categoría' },
          { key: 'price', label: 'Precio', render: (row) => formatCurrency(row.price) },
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
          { key: 'minStock', label: 'Stock mínimo' },
        ]}
        fields={[
          { name: 'productName', label: 'Nombre del producto', required: true },
          { name: 'category', label: 'Categoría', placeholder: 'Ej. Suplementos' },
          { name: 'price', label: 'Precio', type: 'number', min: 0, step: '0.01', defaultValue: 0 },
          { name: 'stock', label: 'Stock inicial', type: 'number', min: 0, defaultValue: 0 },
          { name: 'minStock', label: 'Stock mínimo', type: 'number', min: 0, defaultValue: 5 },
        ]}
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
    </>
  );
}
