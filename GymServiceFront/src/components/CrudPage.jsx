import { useEffect, useState } from 'react';
import {
  AlertCircle,
  CheckCircle2,
  Pencil,
  Plus,
  Search,
  Trash2,
} from 'lucide-react';
import api from '../services/api';
import useApiList from '../hooks/useApiList';
import Table from './ui/Table';
import Modal from './ui/Modal';
import Button from './ui/Button';
import Input from './ui/Input';
import Select from './ui/Select';
import PageHeader from './ui/PageHeader';
import ConfirmDialog from './ui/ConfirmDialog';

const toInputValue = (field, value) => {
  if (value === null || value === undefined) return '';
  if (field.type === 'date') return String(value).slice(0, 10);
  if (field.type === 'datetime-local') return String(value).slice(0, 16);
  return value;
};

const buildPayload = (fields, values) => {
  const payload = {};
  for (const field of fields) {
    const raw = values[field.name];
    if (field.type === 'number') {
      payload[field.name] = raw === '' || raw === null || raw === undefined ? 0 : Number(raw);
    } else {
      payload[field.name] = raw ?? '';
    }
  }
  return payload;
};

/**
 * Página CRUD reutilizable.
 * Las páginas solo describen columnas, campos y rutas; aquí vive la lógica.
 */
export default function CrudPage({
  title,
  subtitle,
  endpoint,
  idKey = 'id',
  columns = [],
  fields = [],
  createFields,
  editFields,
  canCreate = true,
  canEdit = true,
  canDelete = true,
  createLabel = 'Nuevo registro',
  createPath,
  updatePath,
  deletePath,
  createPayload,
  updatePayload,
  mapItemToForm,
  searchParam,
  searchPlaceholder = 'Buscar...',
  actions = [],
  toolbar,
  emptyText,
  emptyIcon,
  modalSize = 'lg',
  hideHeader = false,
  afterCreate,
}) {
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const listParams = searchParam && search ? { [searchParam]: search } : {};
  const {
    items,
    loading,
    error,
    pagination,
    page,
    setPage,
    reload,
  } = useApiList(endpoint, { params: listParams });
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState({});
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);
  const [notice, setNotice] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleting, setDeleting] = useState(false);
  const [pendingAction, setPendingAction] = useState(null);
  const [actionLoading, setActionLoading] = useState(false);

  const activeFields = editing ? (editFields || fields) : (createFields || fields);

  useEffect(() => {
    if (!notice) return undefined;
    const timer = setTimeout(() => setNotice(null), 5000);
    return () => clearTimeout(timer);
  }, [notice]);

  const notify = (type, message) => setNotice({ type, message });

  const openCreate = () => {
    const initial = {};
    for (const field of createFields || fields) {
      initial[field.name] = field.defaultValue ?? '';
    }
    setForm(initial);
    setEditing(null);
    setFormError('');
    setModalOpen(true);
  };

  const openEdit = (item) => {
    const formFields = editFields || fields;
    const initial = {};
    for (const field of formFields) {
      initial[field.name] = toInputValue(field, item[field.name]);
    }
    setForm(mapItemToForm ? mapItemToForm(item, initial) : initial);
    setEditing(item);
    setFormError('');
    setModalOpen(true);
  };

  const closeModal = () => {
    if (saving) return;
    setModalOpen(false);
    setEditing(null);
    setFormError('');
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    const missing = activeFields.filter(
      (field) => field.required && String(form[field.name] ?? '').trim() === ''
    );
    if (missing.length > 0) {
      setFormError(`Completa los campos obligatorios: ${missing.map((f) => f.label).join(', ')}.`);
      return;
    }

    setSaving(true);
    setFormError('');
    try {
      let response;
      if (editing) {
        const path = typeof updatePath === 'function'
          ? updatePath(editing)
          : updatePath || `${endpoint}/${editing[idKey]}`;
        const payload = updatePayload
          ? updatePayload(form, editing)
          : buildPayload(activeFields, form);
        response = await api.put(path, payload);
      } else {
        const path = createPath || endpoint;
        const payload = createPayload
          ? createPayload(form)
          : buildPayload(activeFields, form);
        response = await api.post(path, payload);
      }

      setModalOpen(false);
      setEditing(null);
      notify('success', response.data?.message || 'Registro guardado correctamente.');
      if (!editing && afterCreate) {
        afterCreate(response.data, { notify, reload });
      }
      reload();
    } catch (err) {
      setFormError(err.response?.data?.message || 'No se pudo guardar el registro.');
    } finally {
      setSaving(false);
    }
  };

  const confirmDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      const path = typeof deletePath === 'function'
        ? deletePath(deleteTarget)
        : deletePath || `${endpoint}/${deleteTarget[idKey]}`;
      await api.delete(path);
      notify('success', 'Registro eliminado correctamente.');
      setDeleteTarget(null);
      reload();
    } catch (err) {
      notify('error', err.response?.data?.message || 'No se pudo eliminar el registro.');
    } finally {
      setDeleting(false);
    }
  };

  const handleAction = (action, item) => {
    if (action.confirm) {
      setPendingAction({ action, item });
      return;
    }
    runAction(action, item);
  };

  const runAction = async (action, item) => {
    setActionLoading(true);
    try {
      await action.onClick(item, { reload, notify });
      if (action.closeOnSuccess !== false) setPendingAction(null);
    } catch (err) {
      notify('error', err.response?.data?.message || 'La acción no se pudo completar.');
    } finally {
      setActionLoading(false);
    }
  };

  const renderFormField = (field) => {
    const value = form[field.name] ?? '';
    const handleChange = (event) => {
      setForm((prev) => ({ ...prev, [field.name]: event.target.value }));
    };

    if (field.type === 'select') {
      return (
        <div key={field.name} className={field.full ? 'sm:col-span-2' : ''}>
          <Select
            id={field.name}
            label={field.label}
            value={value}
            onChange={handleChange}
            options={field.options || []}
            placeholder={field.placeholder}
            required={field.required}
          />
          {field.help && <p className="mt-1 text-xs text-slate-400">{field.help}</p>}
        </div>
      );
    }

    if (field.type === 'textarea') {
      return (
        <div key={field.name} className={field.full ? 'sm:col-span-2' : ''}>
          <label htmlFor={field.name} className="block text-sm font-medium text-slate-700 mb-1.5">
            {field.label}
          </label>
          <textarea
            id={field.name}
            value={value}
            onChange={handleChange}
            required={field.required}
            rows={field.rows || 3}
            placeholder={field.placeholder}
            className="block w-full rounded-lg border border-slate-300 bg-white text-sm text-slate-800 placeholder-slate-400 px-3.5 py-2.5 transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-brand-500/50 focus:border-brand-500"
          />
          {field.help && <p className="mt-1 text-xs text-slate-400">{field.help}</p>}
        </div>
      );
    }

    return (
      <div key={field.name} className={field.full ? 'sm:col-span-2' : ''}>
        <Input
          id={field.name}
          label={field.label}
          type={field.type || 'text'}
          value={value}
          onChange={handleChange}
          required={field.required}
          placeholder={field.placeholder}
          min={field.min}
          max={field.max}
          step={field.step}
        />
        {field.help && <p className="mt-1 text-xs text-slate-400">{field.help}</p>}
      </div>
    );
  };

  const tableColumns = columns.map((column) => ({
    header: column.label,
    accessor: column.key,
    render: column.render ? (value, row) => column.render(row, value) : undefined,
  }));

  if (canEdit || canDelete || actions.length > 0) {
    tableColumns.push({
      header: 'Acciones',
      accessor: idKey,
      render: (_value, row) => (
        <div className="flex items-center justify-end gap-1">
          {actions
            .filter((action) => !action.hidden || !action.hidden(row))
            .map((action) => {
              const Icon = action.icon;
              return (
                <button
                  key={action.label}
                  type="button"
                  title={action.label}
                  aria-label={action.label}
                  onClick={() => handleAction(action, row)}
                  className={`p-1.5 rounded-lg transition-colors ${
                    action.variant === 'danger'
                      ? 'text-danger-500 hover:bg-danger-50'
                      : 'text-slate-500 hover:bg-slate-100'
                  }`}
                >
                  {Icon ? <Icon className="w-4 h-4" /> : <span className="text-xs">{action.label}</span>}
                </button>
              );
            })}
          {canEdit && (
            <button
              type="button"
              title="Editar"
              aria-label="Editar"
              onClick={() => openEdit(row)}
              className="p-1.5 rounded-lg text-slate-500 hover:bg-slate-100 transition-colors"
            >
              <Pencil className="w-4 h-4" />
            </button>
          )}
          {canDelete && (
            <button
              type="button"
              title="Eliminar"
              aria-label="Eliminar"
              onClick={() => setDeleteTarget(row)}
              className="p-1.5 rounded-lg text-danger-500 hover:bg-danger-50 transition-colors"
            >
              <Trash2 className="w-4 h-4" />
            </button>
          )}
        </div>
      ),
    });
  }

  return (
    <div className="animate-fade-in">
      {!hideHeader && (
        <PageHeader
          title={title}
          subtitle={subtitle}
          actions={
            <>
              {toolbar && toolbar({ items, reload, notify })}
              {canCreate && (
                <Button icon={Plus} onClick={openCreate} className="w-full sm:w-auto">
                  {createLabel}
                </Button>
              )}
            </>
          }
        />
      )}

      {hideHeader && (toolbar || canCreate) && (
        <div className="flex flex-wrap items-center justify-end gap-2 mb-4">
          {toolbar && toolbar({ items, reload, notify })}
          {canCreate && (
            <Button icon={Plus} onClick={openCreate}>
              {createLabel}
            </Button>
          )}
        </div>
      )}

      {notice && (
        <div
          className={`mb-4 flex items-start gap-2.5 rounded-xl border px-4 py-3 text-sm ${
            notice.type === 'success'
              ? 'bg-success-50 border-success-200 text-success-700'
              : 'bg-danger-50 border-danger-200 text-danger-700'
          }`}
        >
          {notice.type === 'success' ? (
            <CheckCircle2 className="w-4 h-4 mt-0.5 flex-shrink-0" />
          ) : (
            <AlertCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
          )}
          <span>{notice.message}</span>
        </div>
      )}

      {searchParam && (
        <div className="mb-4">
          <form
            onSubmit={(event) => {
              event.preventDefault();
              setPage(1);
              setSearch(searchInput.trim());
            }}
            className="flex items-center gap-2"
          >
            <div className="relative flex-1 sm:max-w-xs">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
              <input
                type="search"
                value={searchInput}
                onChange={(event) => setSearchInput(event.target.value)}
                placeholder={searchPlaceholder}
                aria-label={searchPlaceholder}
                className="w-full rounded-lg border border-slate-300 bg-white pl-9 pr-3 py-2 text-sm text-slate-800 placeholder-slate-400 transition-colors focus:outline-none focus:ring-2 focus:ring-brand-500/50 focus:border-brand-500"
              />
            </div>
            <Button type="submit" variant="outline" size="sm">
              Buscar
            </Button>
          </form>
        </div>
      )}

      {error && (
        <div className="mb-4 flex items-start gap-2.5 rounded-xl border border-danger-200 bg-danger-50 px-4 py-3 text-sm text-danger-700">
          <AlertCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <Table
        columns={tableColumns}
        data={items}
        loading={loading}
        emptyText={emptyText || 'No hay registros disponibles'}
        emptyIcon={emptyIcon}
        emptyAction={
          canCreate ? (
            <Button icon={Plus} size="sm" onClick={openCreate} className="w-full sm:w-auto">
              {createLabel}
            </Button>
          ) : null
        }
      />

      {pagination && pagination.totalPages > 1 && (
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mt-4">
          <p className="text-sm text-slate-500">
            Página {page} de {pagination.totalPages} · {pagination.totalCount} registros
          </p>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={!pagination.hasPrev || loading}
              onClick={() => setPage(page - 1)}
            >
              Anterior
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={!pagination.hasNext || loading}
              onClick={() => setPage(page + 1)}
            >
              Siguiente
            </Button>
          </div>
        </div>
      )}

      <Modal
        isOpen={modalOpen}
        onClose={closeModal}
        title={editing ? `Editar ${title.toLowerCase()}` : createLabel}
        size={modalSize}
      >
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {activeFields.map(renderFormField)}
          </div>

          {formError && (
            <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
              <AlertCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
              <span>{formError}</span>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={closeModal} disabled={saving}>
              Cancelar
            </Button>
            <Button type="submit" loading={saving}>
              {editing ? 'Guardar cambios' : 'Crear'}
            </Button>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!deleteTarget}
        onClose={() => setDeleteTarget(null)}
        onConfirm={confirmDelete}
        loading={deleting}
        title="Eliminar registro"
        message="Esta acción no se puede deshacer. ¿Deseas continuar?"
        confirmLabel="Eliminar"
      />

      <ConfirmDialog
        isOpen={!!pendingAction}
        onClose={() => setPendingAction(null)}
        onConfirm={() => pendingAction && runAction(pendingAction.action, pendingAction.item)}
        loading={actionLoading}
        title={pendingAction?.action.label || 'Confirmar acción'}
        message={pendingAction?.action.confirm || '¿Deseas continuar?'}
        confirmLabel={pendingAction?.action.confirmLabel || 'Continuar'}
        variant={pendingAction?.action.variant === 'danger' ? 'danger' : 'primary'}
      />
    </div>
  );
}
