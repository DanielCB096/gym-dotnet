import { useCallback, useEffect, useState } from 'react';
import {
  AlertCircle,
  CheckCircle2,
  ClipboardList,
  PackagePlus,
  Plus,
  Send,
  Trash2,
  Truck,
  XCircle,
} from 'lucide-react';
import api from '../services/api';
import PageHeader from '../components/ui/PageHeader';
import CrudPage from '../components/CrudPage';
import Modal from '../components/ui/Modal';
import Button from '../components/ui/Button';
import Input from '../components/ui/Input';
import Select from '../components/ui/Select';
import Table from '../components/ui/Table';
import Badge from '../components/ui/Badge';
import ConfirmDialog from '../components/ui/ConfirmDialog';
import { formatCurrency, formatDateTime } from '../utils/format';

const errorMessage = (err, fallback) => err?.response?.data?.message || fallback;

const statusInfo = {
  draft: { variant: 'neutral', label: 'Borrador' },
  sent: { variant: 'info', label: 'Enviada' },
  received: { variant: 'success', label: 'Recibida' },
  cancelled: { variant: 'danger', label: 'Cancelada' },
};

const movementInfo = {
  initial: { variant: 'neutral', label: 'Inicial' },
  purchase: { variant: 'success', label: 'Compra' },
  sale: { variant: 'info', label: 'Venta' },
  return: { variant: 'warning', label: 'Devolución' },
  adjustment: { variant: 'primary', label: 'Ajuste' },
};

function SuppliersPanel() {
  return (
    <CrudPage
      hideHeader
      title="Proveedor"
      subtitle="Proveedores de productos"
      endpoint="/suppliers"
      extraParams={{ includeInactive: true }}
      createLabel="Nuevo proveedor"
      emptyText="No hay proveedores registrados"
      columns={[
        {
          key: 'supplierName',
          label: 'Proveedor',
          render: (row) => (
            <div>
              <p className="font-medium text-slate-800">{row.supplierName}</p>
              <p className="text-xs text-slate-400">{row.contactName || row.email || '—'}</p>
            </div>
          ),
        },
        { key: 'email', label: 'Email', render: (row) => row.email || '—' },
        { key: 'phone', label: 'Teléfono', render: (row) => row.phone || '—' },
        { key: 'rfc', label: 'RFC', render: (row) => row.rfc || '—' },
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
      fields={[
        { name: 'supplierName', label: 'Nombre del proveedor', required: true },
        { name: 'contactName', label: 'Contacto' },
        { name: 'email', label: 'Email', type: 'email' },
        { name: 'phone', label: 'Teléfono' },
        { name: 'rfc', label: 'RFC' },
        { name: 'address', label: 'Dirección', type: 'textarea', full: true, rows: 2 },
      ]}
      editFields={[
        { name: 'supplierName', label: 'Nombre del proveedor', required: true },
        { name: 'contactName', label: 'Contacto' },
        { name: 'email', label: 'Email', type: 'email' },
        { name: 'phone', label: 'Teléfono' },
        { name: 'rfc', label: 'RFC' },
        { name: 'address', label: 'Dirección', type: 'textarea', full: true, rows: 2 },
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
      mapItemToForm={(item, initial) => ({ ...initial, isActive: item.isActive ? 'true' : 'false' })}
    />
  );
}

function PurchaseOrdersPanel() {
  const [orders, setOrders] = useState([]);
  const [suppliers, setSuppliers] = useState([]);
  const [products, setProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [form, setForm] = useState({ supplierId: '', items: [] });
  const [saving, setSaving] = useState(false);
  const [confirm, setConfirm] = useState(null);
  const [confirmLoading, setConfirmLoading] = useState(false);

  const notify = (type, message) => {
    setNotice({ type, message });
    window.clearTimeout(notify.timer);
    notify.timer = window.setTimeout(() => setNotice(null), 4500);
  };

  const loadCatalogs = useCallback(async () => {
    const [suppliersResponse, productsResponse] = await Promise.allSettled([
      api.get('/suppliers', { params: { includeInactive: false } }),
      api.get('/inventory', { params: { limit: 500 } }),
    ]);
    if (suppliersResponse.status === 'fulfilled') setSuppliers(suppliersResponse.value.data?.data || []);
    if (productsResponse.status === 'fulfilled') setProducts(productsResponse.value.data?.data || []);
  }, []);

  const loadOrders = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/purchase-orders');
      setOrders(response.data?.data || []);
    } catch (err) {
      setOrders([]);
      setError(errorMessage(err, 'No se pudieron cargar las órdenes.'));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadCatalogs(); }, [loadCatalogs]);
  useEffect(() => { loadOrders(); }, [loadOrders]);

  const openCreate = () => {
    setForm({
      supplierId: '',
      items: [{ productId: '', quantity: 1, unitCost: '' }],
    });
    setError('');
    setModalOpen(true);
  };

  const changeItem = (index, changes) => {
    setForm((prev) => ({
      ...prev,
      items: prev.items.map((item, i) => (i === index ? { ...item, ...changes } : item)),
    }));
  };

  const total = form.items.reduce(
    (sum, item) => sum + (Number(item.quantity) || 0) * (Number(item.unitCost) || 0),
    0,
  );

  const createOrder = async (event) => {
    event.preventDefault();
    setSaving(true);
    setError('');
    try {
      await api.post('/purchase-orders', {
        supplierId: Number(form.supplierId),
        items: form.items
          .filter((item) => item.productId)
          .map((item) => ({
            productId: Number(item.productId),
            quantity: Number(item.quantity) || 0,
            unitCost: Number(item.unitCost) || 0,
          })),
      });
      notify('success', 'Orden de compra creada.');
      setModalOpen(false);
      await loadOrders();
    } catch (err) {
      setError(errorMessage(err, 'No se pudo crear la orden.'));
    } finally {
      setSaving(false);
    }
  };

  const runAction = async (order, action, message) => {
    try {
      const response = await api.post(`/purchase-orders/${order.id}/${action}`);
      notify('success', response.data?.message || message);
      await Promise.all([loadOrders(), loadCatalogs()]);
    } catch (err) {
      notify('error', errorMessage(err, 'No se pudo completar la acción.'));
    }
  };

  const confirmAction = (order, action, title, message, variant = 'primary') => {
    setConfirm({
      title,
      message,
      variant,
      action: () => runAction(order, action, message),
    });
  };

  const runConfirm = async () => {
    if (!confirm) return;
    setConfirmLoading(true);
    try {
      await confirm.action();
      setConfirm(null);
    } catch (err) {
      notify('error', errorMessage(err, 'No se pudo completar la acción.'));
      setConfirm(null);
    } finally {
      setConfirmLoading(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-slate-500">
          Crea órdenes, envíalas al proveedor y recíbelas para actualizar el inventario.
        </p>
        <Button icon={PackagePlus} onClick={openCreate}>Nueva orden</Button>
      </div>

      {notice && (
        <div className={`flex items-start gap-2 rounded-lg border px-3 py-2 text-sm ${
          notice.type === 'error'
            ? 'border-danger-200 bg-danger-50 text-danger-700'
            : 'border-success-200 bg-success-50 text-success-700'
        }`}>
          {notice.type === 'error'
            ? <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
            : <CheckCircle2 className="h-4 w-4 mt-0.5 flex-shrink-0" />}
          <span>{notice.message}</span>
        </div>
      )}

      {error && (
        <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
          <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <Table
        columns={[
          {
            header: 'Orden',
            accessor: 'orderNumber',
            render: (value, row) => (
              <div>
                <p className="font-medium text-slate-800">{value}</p>
                <p className="text-xs text-slate-400">{formatDateTime(row.orderDate)}</p>
              </div>
            ),
          },
          { header: 'Proveedor', accessor: 'supplierName' },
          { header: 'Productos', accessor: 'itemCount' },
          { header: 'Total', accessor: 'totalAmount', render: (value) => formatCurrency(value) },
          {
            header: 'Estado',
            accessor: 'status',
            render: (value) => {
              const info = statusInfo[value] || { variant: 'neutral', label: value };
              return <Badge variant={info.variant} dot>{info.label}</Badge>;
            },
          },
          {
            header: '',
            accessor: 'id',
            render: (_, row) => (
              <div className="flex items-center justify-end gap-1">
                {row.status === 'draft' && (
                  <Button variant="ghost" size="sm" icon={Send} onClick={() => runAction(row, 'send', 'Orden enviada.')}>
                    Enviar
                  </Button>
                )}
                {(row.status === 'draft' || row.status === 'sent') && (
                  <>
                    <Button
                      variant="ghost"
                      size="sm"
                      icon={Truck}
                      onClick={() => confirmAction(
                        row,
                        'receive',
                        'Recibir orden',
                        'Se sumará la cantidad de cada producto al inventario. ¿Continuar?',
                      )}
                    >
                      Recibir
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      icon={XCircle}
                      onClick={() => confirmAction(
                        row,
                        'cancel',
                        'Cancelar orden',
                        'La orden quedará cancelada y no afectará el inventario. ¿Continuar?',
                        'danger',
                      )}
                    >
                      Cancelar
                    </Button>
                  </>
                )}
              </div>
            ),
          },
        ]}
        data={orders}
        loading={loading}
        emptyText="No hay órdenes de compra"
        emptyIcon={ClipboardList}
      />

      <Modal isOpen={modalOpen} onClose={() => !saving && setModalOpen(false)} title="Nueva orden de compra" size="xl">
        <form onSubmit={createOrder} className="space-y-4">
          <Select
            label="Proveedor"
            value={form.supplierId}
            onChange={(event) => setForm({ ...form, supplierId: event.target.value })}
            options={suppliers.map((supplier) => ({ value: supplier.id, label: supplier.supplierName }))}
            placeholder="Selecciona un proveedor"
            required
          />

          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <p className="text-sm font-medium text-slate-700">Productos</p>
              <Button
                type="button"
                variant="outline"
                size="sm"
                icon={Plus}
                onClick={() => setForm({ ...form, items: [...form.items, { productId: '', quantity: 1, unitCost: '' }] })}
              >
                Agregar
              </Button>
            </div>
            {form.items.map((item, index) => (
              <div key={index} className="grid grid-cols-1 gap-2 rounded-lg border border-slate-200 p-3 sm:grid-cols-12">
                <div className="sm:col-span-5">
                  <Select
                    label="Producto"
                    value={item.productId}
                    onChange={(event) => changeItem(index, { productId: event.target.value })}
                    options={products.map((product) => ({
                      value: product.id,
                      label: `${product.productName} · stock ${product.stock}`,
                    }))}
                    placeholder="Producto"
                    required
                  />
                </div>
                <div className="sm:col-span-2">
                  <Input
                    label="Cantidad"
                    type="number"
                    min="1"
                    value={item.quantity}
                    onChange={(event) => changeItem(index, { quantity: event.target.value })}
                    required
                  />
                </div>
                <div className="sm:col-span-3">
                  <Input
                    label="Costo unitario"
                    type="number"
                    min="0"
                    step="0.01"
                    value={item.unitCost}
                    onChange={(event) => changeItem(index, { unitCost: event.target.value })}
                    required
                  />
                </div>
                <div className="flex items-end sm:col-span-2">
                  <Button
                    type="button"
                    variant="ghost"
                    icon={Trash2}
                    disabled={form.items.length === 1}
                    onClick={() => setForm({ ...form, items: form.items.filter((_, i) => i !== index) })}
                  >
                    Quitar
                  </Button>
                </div>
              </div>
            ))}
          </div>

          <div className="flex items-center justify-between rounded-lg bg-slate-50 px-4 py-3">
            <span className="text-sm text-slate-500">Total de la orden</span>
            <span className="text-lg font-bold text-slate-800">{formatCurrency(total)}</span>
          </div>

          {error && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={() => setModalOpen(false)} disabled={saving}>Cancelar</Button>
            <Button type="submit" loading={saving} disabled={!form.supplierId || !form.items.some((item) => item.productId)}>
              Crear orden
            </Button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!confirm}
        onClose={() => setConfirm(null)}
        onConfirm={runConfirm}
        loading={confirmLoading}
        title={confirm?.title || 'Confirmar'}
        message={confirm?.message || '¿Deseas continuar?'}
        confirmLabel="Continuar"
        variant={confirm?.variant === 'danger' ? 'danger' : 'primary'}
      />
    </div>
  );
}

function MovementsPanel() {
  const [movements, setMovements] = useState([]);
  const [products, setProducts] = useState([]);
  const [filters, setFilters] = useState({ productId: '', type: '' });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    api.get('/inventory', { params: { limit: 500 } })
      .then((response) => setProducts(response.data?.data || []))
      .catch(() => setProducts([]));
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const params = { limit: 200 };
      if (filters.productId) params.productId = Number(filters.productId);
      if (filters.type) params.type = filters.type;
      const response = await api.get('/inventory/movements', { params });
      setMovements(response.data?.data || []);
    } catch (err) {
      setMovements([]);
      setError(errorMessage(err, 'No se pudieron cargar los movimientos.'));
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => { load(); }, [load]);

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:flex-row sm:items-end">
        <Select
          label="Producto"
          value={filters.productId}
          onChange={(event) => setFilters({ ...filters, productId: event.target.value })}
          options={products.map((product) => ({ value: product.id, label: product.productName }))}
          placeholder="Todos"
        />
        <Select
          label="Tipo"
          value={filters.type}
          onChange={(event) => setFilters({ ...filters, type: event.target.value })}
          options={[
            { value: 'initial', label: 'Inicial' },
            { value: 'purchase', label: 'Compra' },
            { value: 'sale', label: 'Venta' },
            { value: 'return', label: 'Devolución' },
            { value: 'adjustment', label: 'Ajuste' },
          ]}
          placeholder="Todos"
        />
        <Button variant="outline" onClick={load} className="sm:mb-0.5">Actualizar</Button>
      </div>

      {error && (
        <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
          <AlertCircle className="h-4 w-4 mt-0.5 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <Table
        columns={[
          { header: 'Fecha', accessor: 'movementDate', render: (value) => formatDateTime(value) },
          { header: 'Producto', accessor: 'productName' },
          {
            header: 'Tipo',
            accessor: 'movementType',
            render: (value) => {
              const info = movementInfo[value] || { variant: 'neutral', label: value };
              return <Badge variant={info.variant}>{info.label}</Badge>;
            },
          },
          {
            header: 'Cambio',
            accessor: 'quantityDelta',
            render: (value) => (
              <span className={value >= 0 ? 'font-medium text-success-600' : 'font-medium text-danger-600'}>
                {value > 0 ? `+${value}` : value}
              </span>
            ),
          },
          { header: 'Anterior', accessor: 'previousStock' },
          { header: 'Nuevo', accessor: 'newStock' },
          { header: 'Referencia', accessor: 'referenceType', render: (value, row) => (value ? `${value} #${row.referenceId}` : '—') },
          { header: 'Notas', accessor: 'notes', render: (value) => value || '—' },
        ]}
        data={movements}
        loading={loading}
        emptyText="No hay movimientos de inventario"
        emptyIcon={ClipboardList}
      />
    </div>
  );
}

export default function Procurement() {
  const [tab, setTab] = useState('orders');
  const tabs = [
    { id: 'orders', label: 'Órdenes de compra' },
    { id: 'suppliers', label: 'Proveedores' },
    { id: 'movements', label: 'Movimientos' },
  ];

  return (
    <div className="animate-fade-in">
      <PageHeader
        title="Compras e inventario"
        subtitle="Proveedores, órdenes de compra y kardex de movimientos"
      />

      <div className="mb-6 flex flex-wrap gap-1 rounded-lg border border-slate-200 bg-white p-1 shadow-sm">
        {tabs.map((item) => (
          <button
            key={item.id}
            type="button"
            onClick={() => setTab(item.id)}
            className={`rounded-md px-4 py-2 text-sm font-medium transition-colors ${
              tab === item.id ? 'bg-brand-600 text-white' : 'text-slate-600 hover:bg-slate-50'
            }`}
          >
            {item.label}
          </button>
        ))}
      </div>

      {tab === 'orders' && <PurchaseOrdersPanel />}
      {tab === 'suppliers' && <SuppliersPanel />}
      {tab === 'movements' && <MovementsPanel />}
    </div>
  );
}
