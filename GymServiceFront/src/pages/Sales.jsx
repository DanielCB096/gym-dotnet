import { useState } from 'react';
import { AlertCircle, Plus, RotateCcw, ShoppingCart, Trash2 } from 'lucide-react';
import api from '../services/api';
import CrudPage from '../components/CrudPage';
import Badge from '../components/ui/Badge';
import Modal from '../components/ui/Modal';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Select from '../components/ui/Select';
import { formatCurrency, formatDateTime, humanize } from '../utils/format';
import { Printer } from 'lucide-react';

const TAX_RATE = 0.16;

const emptyItem = () => ({ productId: '', quantity: 1 });

function NewSaleButton({ onDone }) {
  const [open, setOpen] = useState(false);
  const [members, setMembers] = useState([]);
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState('');
  const [form, setForm] = useState({ memberId: '', paymentMethod: 'cash', items: [emptyItem()] });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const loadCatalogs = async () => {
    setLoading(true);
    setLoadError('');
    try {
      const [membersResponse, productsResponse] = await Promise.all([
        api.get('/members', { params: { limit: 200 } }),
        api.get('/inventory', { params: { limit: 200 } }),
      ]);
      const memberList = membersResponse.data?.data || membersResponse.data?.items || [];
      const productList = productsResponse.data?.data || productsResponse.data?.items || [];
      setMembers(Array.isArray(memberList) ? memberList : []);
      setProducts(Array.isArray(productList) ? productList : []);
    } catch (err) {
      setLoadError(err.response?.data?.message || 'No se pudieron cargar socios y productos.');
    } finally {
      setLoading(false);
    }
  };

  const openModal = () => {
    setForm({ memberId: '', paymentMethod: 'cash', items: [emptyItem()] });
    setError('');
    setOpen(true);
    loadCatalogs();
  };

  const closeModal = () => {
    if (saving) return;
    setOpen(false);
  };

  const updateItem = (index, changes) => {
    setForm((prev) => ({
      ...prev,
      items: prev.items.map((item, i) => (i === index ? { ...item, ...changes } : item)),
    }));
  };

  const addItem = () => setForm((prev) => ({ ...prev, items: [...prev.items, emptyItem()] }));

  const removeItem = (index) => {
    setForm((prev) => ({
      ...prev,
      items: prev.items.length === 1 ? prev.items : prev.items.filter((_, i) => i !== index),
    }));
  };

  const priceOf = (productId) => {
    const product = products.find((item) => String(item.id) === String(productId));
    return product ? Number(product.price) : 0;
  };

  const stockOf = (productId) => {
    const product = products.find((item) => String(item.id) === String(productId));
    return product ? Number(product.stock ?? 0) : 0;
  };

  const nameOf = (productId) => {
    const product = products.find((item) => String(item.id) === String(productId));
    return product?.productName || `Producto ${productId}`;
  };

  const subtotal = form.items.reduce(
    (sum, item) => sum + priceOf(item.productId) * (Number(item.quantity) || 0),
    0
  );
  const tax = subtotal * TAX_RATE;
  const total = subtotal + tax;

  const handleSubmit = async (event) => {
    event.preventDefault();
    const items = form.items
      .filter((item) => item.productId && Number(item.quantity) > 0)
      .map((item) => ({
        productId: Number(item.productId),
        quantity: Number(item.quantity),
        price: priceOf(item.productId),
      }));

    if (items.length === 0) {
      setError('Agrega al menos un producto con cantidad mayor a cero.');
      return;
    }

    const withoutStock = items.find((item) => item.quantity > stockOf(item.productId));
    if (withoutStock) {
      setError(
        `Stock insuficiente de '${nameOf(withoutStock.productId)}': disponible ${stockOf(withoutStock.productId)}, solicitado ${withoutStock.quantity}.`
      );
      return;
    }

    setSaving(true);
    setError('');
    try {
      const response = await api.post('/sales', {
        memberId: Number(form.memberId) || 0,
        paymentMethod: form.paymentMethod,
        totalAmount: Number(total.toFixed(2)),
        taxAmount: Number(tax.toFixed(2)),
        isOffline: false,
        items,
      });
      onDone(response.data?.message || 'Venta registrada correctamente.');
      setOpen(false);
    } catch (err) {
      setError(err.response?.data?.message || 'No se pudo registrar la venta.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <Button icon={ShoppingCart} onClick={openModal}>
        Nueva venta
      </Button>

      <Modal isOpen={open} onClose={closeModal} title="Nueva venta" size="lg">
        <form onSubmit={handleSubmit} className="space-y-4">
          {loading && <p className="text-sm text-slate-500">Cargando socios y productos...</p>}

          {loadError && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
              <span>{loadError}</span>
            </div>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Select
              id="memberId"
              label="Socio"
              value={form.memberId}
              onChange={(event) => setForm({ ...form, memberId: event.target.value })}
              placeholder="Público general"
              options={members.map((member) => ({
                value: member.id,
                label: `${member.firstName} ${member.lastName}`.trim() || `Socio #${member.id}`,
              }))}
            />
            <Select
              id="paymentMethod"
              label="Método de pago"
              value={form.paymentMethod}
              onChange={(event) => setForm({ ...form, paymentMethod: event.target.value })}
              options={[
                { value: 'cash', label: 'Efectivo' },
                { value: 'card', label: 'Tarjeta' },
                { value: 'transfer', label: 'Transferencia' },
              ]}
            />
          </div>

          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <h4 className="text-sm font-semibold text-slate-700">Productos</h4>
              <Button
                type="button"
                variant="outline"
                size="sm"
                icon={Plus}
                onClick={addItem}
                disabled={products.length === 0}
              >
                Agregar producto
              </Button>
            </div>

            {products.length === 0 && !loading && (
              <p className="text-sm text-slate-500">
                No hay productos en inventario. Registra productos en la sección Inventario para poder vender.
              </p>
            )}

            {form.items.map((item, index) => (
              <div key={index} className="flex items-start gap-2">
                <div className="flex-1">
                  <Select
                    id={`product-${index}`}
                    label="Producto"
                    value={item.productId}
                    onChange={(event) => updateItem(index, { productId: event.target.value })}
                    placeholder="Selecciona un producto"
                    options={products.map((product) => ({
                      value: product.id,
                      label: `${product.productName} — ${formatCurrency(product.price)} (stock ${product.stock ?? 0})`,
                    }))}
                  />
                </div>
                <div className="w-24">
                  <Input
                    id={`quantity-${index}`}
                    label="Cantidad"
                    type="number"
                    min="1"
                    step="1"
                    value={item.quantity}
                    onChange={(event) => updateItem(index, { quantity: event.target.value })}
                  />
                </div>
                <button
                  type="button"
                  aria-label="Quitar producto"
                  title="Quitar producto"
                  onClick={() => removeItem(index)}
                  disabled={form.items.length === 1}
                  className="mt-7 p-2 rounded-lg text-danger-500 hover:bg-danger-50 transition-colors disabled:opacity-40 disabled:hover:bg-transparent"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            ))}
          </div>

          <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm">
            <div className="flex justify-between text-slate-600">
              <span>Subtotal</span>
              <span>{formatCurrency(subtotal)}</span>
            </div>
            <div className="flex justify-between text-slate-600 mt-1">
              <span>IVA (16%)</span>
              <span>{formatCurrency(tax)}</span>
            </div>
            <div className="flex justify-between font-semibold text-slate-800 mt-2 pt-2 border-t border-slate-200">
              <span>Total</span>
              <span>{formatCurrency(total)}</span>
            </div>
          </div>

          {error && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={closeModal} disabled={saving}>
              Cancelar
            </Button>
            <Button type="submit" loading={saving} disabled={loading}>
              Registrar venta
            </Button>
          </div>
        </form>
      </Modal>
    </>
  );
}

export default function Sales() {
  return (
    <CrudPage
      title="Ventas"
      subtitle="Historial de ventas de mostrador"
      endpoint="/sales"
      emptyText="No hay ventas registradas"
      canCreate={false}
      canEdit={false}
      columns={[
        {
          key: 'saleNumber',
          label: 'No. venta',
          render: (row) => (
            <span className="inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-medium bg-brand-50 text-brand-700">
              {row.saleNumber || '—'}
            </span>
          ),
        },
        { key: 'memberName', label: 'Socio', render: (row) => row.memberName?.trim() || 'Público general' },
        { key: 'totalAmount', label: 'Total', render: (row) => formatCurrency(row.totalAmount) },
        { key: 'paymentMethod', label: 'Método', render: (row) => humanize(row.paymentMethod) },
        {
          key: 'isOffline',
          label: 'Origen',
          render: (row) => (
            <Badge variant={row.isOffline ? 'warning' : 'neutral'}>
              {row.isOffline ? 'Sin conexión' : 'En línea'}
            </Badge>
          ),
        },
        {
          key: 'status',
          label: 'Estado',
          render: (row) => {
            const returned = row.status === 'returned';
            return (
              <Badge variant={returned ? 'danger' : 'success'} dot>
                {returned ? 'Devuelta' : 'Completada'}
              </Badge>
            );
          },
        },
        { key: 'saleDate', label: 'Fecha', render: (row) => formatDateTime(row.saleDate) },
      ]}
      actions={[
        {
          label: 'Recibo',
          icon: Printer,
          onClick: async (row) => {
            const receiptWindow = window.open('', '_blank');
            const response = await api.get(`/sales/${row.id}/receipt`, { responseType: 'text' });
            const blob = new Blob([response.data], { type: 'text/html;charset=utf-8' });
            const url = URL.createObjectURL(blob);
            if (receiptWindow) {
              receiptWindow.document.open();
              receiptWindow.document.write(response.data);
              receiptWindow.document.close();
            } else {
              window.open(url, '_blank');
            }
          },
        },
        {
          label: 'Registrar devolución',
          icon: RotateCcw,
          variant: 'danger',
          confirm:
            'Se repondrá el inventario de todos los productos de la venta y quedará marcada como devuelta. ¿Continuar?',
          confirmLabel: 'Registrar devolución',
          hidden: (row) => row.status === 'returned',
          onClick: async (row, { reload, notify }) => {
            const response = await api.post(`/sales/${row.id}/return`);
            notify('success', response.data?.message || 'Devolución registrada.');
            reload();
          },
        },
      ]}
      toolbar={({ reload, notify }) => (
        <NewSaleButton
          onDone={(message) => {
            notify('success', message);
            reload();
          }}
        />
      )}
    />
  );
}
