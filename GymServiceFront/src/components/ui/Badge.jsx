const variants = {
  success: 'bg-success-50 text-success-600 border-success-200',
  danger: 'bg-danger-50 text-danger-600 border-danger-200',
  warning: 'bg-warning-50 text-warning-600 border-warning-200',
  info: 'bg-info-50 text-info-600 border-info-200',
  primary: 'bg-brand-50 text-brand-600 border-brand-200',
  neutral: 'bg-slate-50 text-slate-600 border-slate-200',
};

const dotColors = {
  success: 'bg-success-500',
  danger: 'bg-danger-500',
  warning: 'bg-warning-500',
  info: 'bg-info-500',
  primary: 'bg-brand-500',
  neutral: 'bg-slate-400',
};

const Badge = ({ variant = 'neutral', dot, children, className = '' }) => {
  return (
    <span
      className={`
        inline-flex items-center gap-1.5 px-2.5 py-1 text-xs font-medium rounded-md
        border
        ${variants[variant] || variants.neutral}
        ${className}
      `}
    >
      {dot && <span className={`w-1.5 h-1.5 rounded-full ${dotColors[variant] || dotColors.neutral}`} />}
      {children}
    </span>
  );
};

export default Badge;
