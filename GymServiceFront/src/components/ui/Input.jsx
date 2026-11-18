import { forwardRef } from 'react';

const Input = forwardRef(function Input(
  { label, icon: Icon, error, disabled, loading, type = 'text', id, className = '', ...props },
  ref
) {
  const inputId = id || label?.toLowerCase().replace(/\s+/g, '-');

  return (
    <div className={`w-full ${className}`}>
      {label && (
        <label htmlFor={inputId} className="block text-sm font-medium text-slate-700 mb-1.5">
          {label}
        </label>
      )}
      <div className="relative">
        {Icon && (
          <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-slate-400">
            <Icon className="w-4 h-4" />
          </div>
        )}
        <input
          ref={ref}
          id={inputId}
          type={type}
          disabled={disabled || loading}
          className={`
            block w-full rounded-lg border border-slate-300 bg-white
            text-sm text-slate-800 placeholder-slate-400
            transition-colors duration-200
            focus:outline-none focus:ring-2 focus:ring-brand-500/50 focus:border-brand-500
            disabled:opacity-50 disabled:cursor-not-allowed
            ${Icon ? 'pl-10' : 'pl-3.5'}
            pr-3.5 py-2.5
            ${error ? 'border-danger-300 focus:ring-danger-500/50 focus:border-danger-500' : ''}
          `}
          {...props}
        />
        {loading && (
          <div className="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
            <svg className="animate-spin h-4 w-4 text-slate-400" viewBox="0 0 24 24">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
              <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
            </svg>
          </div>
        )}
      </div>
      {error && (
        <p className="mt-1.5 text-xs text-danger-500">{error}</p>
      )}
    </div>
  );
});

export default Input;
