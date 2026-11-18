import { forwardRef } from 'react';

const variants = {
  primary: 'bg-brand-500 text-white hover:bg-brand-600 border border-brand-500 shadow-sm hover:shadow-md',
  outline: 'bg-transparent text-brand-500 border border-brand-500 hover:bg-brand-50',
  ghost: 'bg-transparent text-slate-600 hover:bg-slate-100 border border-transparent',
  danger: 'bg-danger-500 text-white hover:bg-danger-600 border border-danger-500 shadow-sm hover:shadow-md',
  success: 'bg-success-500 text-white hover:bg-success-600 border border-success-500 shadow-sm hover:shadow-md',
  warning: 'bg-warning-500 text-white hover:bg-warning-600 border border-warning-500 shadow-sm hover:shadow-md',
};

const sizes = {
  sm: 'px-3 py-1.5 text-xs gap-1.5',
  md: 'px-4 py-2 text-sm gap-2',
  lg: 'px-6 py-2.5 text-base gap-2',
};

const Button = forwardRef(function Button(
  { variant = 'primary', size = 'md', icon: Icon, loading, children, className = '', disabled, ...props },
  ref
) {
  return (
    <button
      ref={ref}
      type={props.type || 'button'}
      disabled={disabled || loading}
      className={`
        inline-flex items-center justify-center font-medium rounded-lg
        transition-all duration-200 ease-in-out
        focus:outline-none focus:ring-2 focus:ring-brand-500/50 focus:ring-offset-1
        disabled:opacity-50 disabled:cursor-not-allowed disabled:pointer-events-none
        ${variants[variant] || variants.primary}
        ${sizes[size]}
        ${className}
      `}
      {...props}
    >
      {loading ? (
        <>
          <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
          </svg>
          <span>Cargando...</span>
        </>
      ) : (
        <>
          {Icon && <Icon className="w-4 h-4 flex-shrink-0" />}
          {children}
        </>
      )}
    </button>
  );
});

export default Button;
