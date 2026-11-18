const Card = ({ title, subtitle, actions, children, className = '', headerClassName = '' }) => {
  return (
    <div className={`bg-white rounded-xl border border-slate-200 shadow-sm ${className}`}>
      {(title || subtitle || actions) && (
        <div className={`flex items-center justify-between px-6 py-4 border-b border-slate-100 ${headerClassName}`}>
          <div>
            {title && <h3 className="text-base font-semibold text-slate-800">{title}</h3>}
            {subtitle && <p className="text-sm text-slate-500 mt-0.5">{subtitle}</p>}
          </div>
          {actions && <div className="flex items-center gap-2">{actions}</div>}
        </div>
      )}
      <div className="p-6">{children}</div>
    </div>
  );
};

export default Card;
