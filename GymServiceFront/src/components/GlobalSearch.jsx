import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, Users, Tags, FileText, X } from 'lucide-react';
import api from '../services/api';

const typeMeta = {
  member: { icon: Users, label: 'Socio' },
  plan: { icon: Tags, label: 'Plan' },
  invoice: { icon: FileText, label: 'Factura' },
};

export default function GlobalSearch() {
  const navigate = useNavigate();
  const containerRef = useRef(null);
  const [query, setQuery] = useState('');
  const [results, setResults] = useState([]);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const handler = (event) => {
      if (containerRef.current && !containerRef.current.contains(event.target)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  useEffect(() => {
    const term = query.trim();
    if (term.length < 2) {
      setResults([]);
      setLoading(false);
      setOpen(false);
      return undefined;
    }

    let active = true;
    const timer = window.setTimeout(async () => {
      setLoading(true);
      const [members, plans, invoices] = await Promise.allSettled([
        api.get('/members', { params: { query: term, limit: 5 } }),
        api.get('/members/plans'),
        api.get('/billing/invoices', { params: { query: term, limit: 5 } }),
      ]);

      if (!active) return;

      const next = [];
      if (members.status === 'fulfilled') {
        (members.value.data?.data || []).forEach((member) => {
          next.push({
            type: 'member',
            label: `${member.firstName || ''} ${member.lastName || ''}`.trim() || `Socio #${member.id}`,
            detail: member.email || member.phone || 'Socio',
            to: '/members',
          });
        });
      }
      if (plans.status === 'fulfilled') {
        const termLower = term.toLowerCase();
        (plans.value.data?.data || [])
          .filter((plan) => String(plan.planName || '').toLowerCase().includes(termLower))
          .slice(0, 5)
          .forEach((plan) => {
            next.push({
              type: 'plan',
              label: plan.planName,
              detail: plan.durationDays ? `${plan.durationDays} días` : 'Plan de membresía',
              to: '/plans',
            });
          });
      }
      if (invoices.status === 'fulfilled') {
        (invoices.value.data?.data || []).forEach((invoice) => {
          next.push({
            type: 'invoice',
            label: `Factura ${invoice.invoiceNumber || `#${invoice.id}`}`,
            detail: invoice.memberName || 'Factura',
            to: '/billing',
          });
        });
      }

      setResults(next.slice(0, 12));
      setLoading(false);
      setOpen(true);
    }, 250);

    return () => {
      active = false;
      window.clearTimeout(timer);
    };
  }, [query]);

  const select = (item) => {
    setOpen(false);
    setQuery('');
    navigate(item.to);
  };

  return (
    <div ref={containerRef} className="relative hidden md:block">
      <div className="flex items-center gap-2 px-3 py-2 rounded-lg border border-slate-200 bg-slate-50 w-48 lg:w-64 focus-within:border-brand-400 focus-within:bg-white transition-colors">
        <Search className="w-4 h-4 text-slate-400 flex-shrink-0" />
        <input
          type="text"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          onFocus={() => { if (results.length > 0) setOpen(true); }}
          onKeyDown={(event) => {
            if (event.key === 'Escape') setOpen(false);
            if (event.key === 'Enter' && results.length > 0) select(results[0]);
          }}
          placeholder="Buscar socio, plan, factura..."
          className="bg-transparent text-sm text-slate-600 placeholder-slate-400 outline-none w-full min-w-0"
          aria-label="Búsqueda global"
        />
        {query && (
          <button
            type="button"
            onClick={() => { setQuery(''); setResults([]); setOpen(false); }}
            className="text-slate-400 hover:text-slate-600"
            aria-label="Limpiar búsqueda"
          >
            <X className="w-3.5 h-3.5" />
          </button>
        )}
      </div>

      {open && (
        <div className="absolute right-0 top-full z-50 mt-2 w-80 max-w-[calc(100vw-2rem)] overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xl">
          {loading ? (
            <p className="p-4 text-sm text-slate-500">Buscando...</p>
          ) : results.length === 0 ? (
            <p className="p-4 text-sm text-slate-500">Sin resultados para “{query.trim()}”.</p>
          ) : (
            <ul className="max-h-80 overflow-y-auto py-1">
              {results.map((item, index) => {
                const meta = typeMeta[item.type] || typeMeta.member;
                const Icon = meta.icon;
                return (
                  <li key={`${item.type}-${index}`}>
                    <button
                      type="button"
                      onClick={() => select(item)}
                      className="flex w-full items-center gap-3 px-4 py-2.5 text-left hover:bg-slate-50 transition-colors"
                    >
                      <span className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-lg bg-slate-100 text-slate-500">
                        <Icon className="h-4 w-4" />
                      </span>
                      <span className="min-w-0 flex-1">
                        <span className="block truncate text-sm font-medium text-slate-800">{item.label}</span>
                        <span className="block truncate text-xs text-slate-400">{item.detail}</span>
                      </span>
                      <span className="text-[10px] uppercase tracking-wide text-slate-400">{meta.label}</span>
                    </button>
                  </li>
                );
              })}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
