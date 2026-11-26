const formatShortDate = (value) => {
  const date = value ? new Date(value) : null;
  if (!date || Number.isNaN(date.getTime())) return '';
  return date.toLocaleDateString('es-MX', { day: '2-digit', month: 'short' });
};

export default function ProgressChart({ points = [], unit = '', color = '#465fff', height = 220 }) {
  const valid = points
    .map((point) => ({ ...point, value: Number(point.value) }))
    .filter((point) => Number.isFinite(point.value))
    .sort((a, b) => new Date(a.date) - new Date(b.date));

  if (valid.length === 0) return null;

  const width = 640;
  const padding = { top: 18, right: 18, bottom: 34, left: 48 };
  const plotWidth = width - padding.left - padding.right;
  const plotHeight = height - padding.top - padding.bottom;

  let min = Math.min(...valid.map((point) => point.value));
  let max = Math.max(...valid.map((point) => point.value));
  if (min === max) {
    const pad = Math.abs(min) * 0.1 || 1;
    min -= pad;
    max += pad;
  }

  const x = (index) => padding.left + (valid.length === 1
    ? plotWidth / 2
    : (index * plotWidth) / (valid.length - 1));
  const y = (value) => padding.top + (1 - (value - min) / (max - min)) * plotHeight;

  const line = valid.map((point, index) => `${index === 0 ? 'M' : 'L'} ${x(index).toFixed(1)} ${y(point.value).toFixed(1)}`).join(' ');
  const area = `${line} L ${x(valid.length - 1).toFixed(1)} ${(padding.top + plotHeight).toFixed(1)} L ${x(0).toFixed(1)} ${(padding.top + plotHeight).toFixed(1)} Z`;
  const gridValues = [max, (max + min) / 2, min];
  const last = valid[valid.length - 1];
  const first = valid[0];
  const delta = last.value - first.value;

  return (
    <div>
      <div className="mb-2 flex flex-wrap items-end justify-between gap-2">
        <div className="flex items-baseline gap-2">
          <span className="text-2xl font-bold text-slate-800">{last.value}</span>
          <span className="text-xs text-slate-400">{unit}</span>
        </div>
        <div className={`text-xs font-medium ${delta > 0 ? 'text-success-600' : delta < 0 ? 'text-danger-600' : 'text-slate-400'}`}>
          {delta > 0 ? '+' : ''}{delta.toFixed(2)} {unit} desde la primera medición
        </div>
      </div>

      <svg
        viewBox={`0 0 ${width} ${height}`}
        className="h-auto w-full"
        role="img"
        aria-label={`Gráfica de progreso en ${unit || 'unidades'}`}
      >
        {gridValues.map((value, index) => (
          <g key={index}>
            <line
              x1={padding.left}
              x2={width - padding.right}
              y1={y(value)}
              y2={y(value)}
              stroke="#e2e8f0"
              strokeWidth="1"
            />
            <text x={padding.left - 8} y={y(value) + 3} textAnchor="end" fontSize="10" fill="#94a3b8">
              {Number(value.toFixed(1))}
            </text>
          </g>
        ))}

        <path d={area} fill={color} opacity="0.1" />
        <path d={line} fill="none" stroke={color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" />

        {valid.map((point, index) => (
          <circle key={index} cx={x(index)} cy={y(point.value)} r="4" fill="#fff" stroke={color} strokeWidth="2">
            <title>{`${formatShortDate(point.date)}: ${point.value} ${unit}`}</title>
          </circle>
        ))}

        <text x={padding.left} y={height - 10} fontSize="10" fill="#94a3b8" textAnchor="start">
          {formatShortDate(first.date)}
        </text>
        {valid.length > 1 && (
          <text x={width - padding.right} y={height - 10} fontSize="10" fill="#94a3b8" textAnchor="end">
            {formatShortDate(last.date)}
          </text>
        )}
      </svg>
    </div>
  );
}
