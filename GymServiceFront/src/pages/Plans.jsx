import CrudPage from '../components/CrudPage';
import Badge from '../components/ui/Badge';
import { formatCurrency } from '../utils/format';

const booleanOptions = [
  { value: 'true', label: 'Sí' },
  { value: 'false', label: 'No' },
];

const toBooleanString = (value) => (value ? 'true' : 'false');

export default function Plans() {
  const baseFields = [
    { name: 'planName', label: 'Nombre del plan', required: true, placeholder: 'Ej. Mensual' },
    { name: 'description', label: 'Descripción', type: 'textarea', full: true, rows: 2 },
    { name: 'price', label: 'Precio', type: 'number', min: 0, step: '0.01', defaultValue: 0 },
    {
      name: 'currency',
      label: 'Moneda',
      type: 'select',
      defaultValue: 'MXN',
      options: [
        { value: 'MXN', label: 'MXN' },
        { value: 'USD', label: 'USD' },
        { value: 'EUR', label: 'EUR' },
      ],
    },
    { name: 'durationDays', label: 'Duración (días)', type: 'number', min: 1, defaultValue: 30 },
    {
      name: 'maxDailyEntries',
      label: 'Entradas por día',
      type: 'number',
      defaultValue: -1,
      help: 'Usa -1 o 0 para sin límite.',
    },
    { name: 'includesClasses', label: 'Incluye clases', type: 'select', options: booleanOptions, defaultValue: 'true' },
    { name: 'includesPremiumClasses', label: 'Incluye clases premium', type: 'select', options: booleanOptions, defaultValue: 'false' },
  ];

  return (
    <CrudPage
      title="Planes de membresía"
      subtitle="Precios, duración, beneficios y límite diario de entradas"
      endpoint="/members/plans"
      extraParams={{ includeInactive: true }}
      createLabel="Nuevo plan"
      emptyText="No hay planes registrados"
      canDelete={false}
      columns={[
        {
          key: 'planName',
          label: 'Plan',
          render: (row) => (
            <div>
              <p className="font-medium text-slate-800">{row.planName}</p>
              <p className="text-xs text-slate-400">{row.description || '—'}</p>
            </div>
          ),
        },
        { key: 'price', label: 'Precio', render: (row) => formatCurrency(row.price, row.currency || 'MXN') },
        { key: 'durationDays', label: 'Duración', render: (row) => `${row.durationDays} días` },
        {
          key: 'maxDailyEntries',
          label: 'Entradas/día',
          render: (row) => (row.maxDailyEntries > 0 ? row.maxDailyEntries : 'Sin límite'),
        },
        {
          key: 'includesClasses',
          label: 'Clases',
          render: (row) => (row.includesClasses ? 'Sí' : 'No'),
        },
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
      fields={baseFields}
      createFields={[
        ...baseFields,
        { name: 'isActive', label: 'Activo', type: 'select', options: booleanOptions, defaultValue: 'true' },
      ]}
      editFields={[
        ...baseFields,
        { name: 'isActive', label: 'Activo', type: 'select', options: booleanOptions },
      ]}
      mapItemToForm={(item, initial) => ({
        ...initial,
        includesClasses: toBooleanString(item.includesClasses),
        includesPremiumClasses: toBooleanString(item.includesPremiumClasses),
        isActive: toBooleanString(item.isActive),
      })}
    />
  );
}
