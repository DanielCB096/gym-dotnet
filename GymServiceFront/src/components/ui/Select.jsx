import { forwardRef } from 'react';

const Select = forwardRef(function Select(
  { label, error, id, options = [], placeholder, className = '', children, ...props },
  ref
) {
  const selectId = id || label?.toLowerCase().replace(/\s+/g, '-');

  return (
    <div className={`w-full ${className}`}>
      {label && (
        <label htmlFor={selectId} className="block text-sm font-medium text-slate-700 mb-1.5">
          {label}
        </label>
      )}
      <select
        ref={ref}
        id={selectId}
        className={`
          block w-full rounded-lg border border-slate-300 bg-white
          text-sm text-slate-800 px-3.5 py-2.5
          transition-colors duration-200
          focus:outline-none focus:ring-2 focus:ring-brand-500/50 focus:border-brand-500
          disabled:opacity-50 disabled:cursor-not-allowed
          ${error ? 'border-danger-300 focus:ring-danger-500/50 focus:border-danger-500' : ''}
        `}
        {...props}
      >
        {placeholder && <option value="">{placeholder}</option>}
        {children}
        {options.map((opt) => {
          const value = typeof opt === 'string' ? opt : opt.value;
          const labelText = typeof opt === 'string' ? opt : opt.label;
          return (
            <option key={value} value={value}>
              {labelText}
            </option>
          );
        })}
      </select>
      {error && <p className="mt-1.5 text-xs text-danger-500">{error}</p>}
    </div>
  );
});

export default Select;
