import { useState } from 'react';
import PageHeader from '../components/ui/PageHeader';
import MaintenanceEquipmentPanel from '../components/MaintenanceEquipmentPanel';
import MaintenanceTicketsPanel from '../components/MaintenanceTicketsPanel';

export default function Maintenance() {
  const [tab, setTab] = useState('equipment');
  const [ticketEquipment, setTicketEquipment] = useState('');

  const reportTicket = (equipmentId) => {
    setTicketEquipment(equipmentId ? String(equipmentId) : '');
    setTab('tickets');
  };

  return (
    <div className="animate-fade-in">
      <PageHeader
        title="Mantenimiento"
        subtitle="Equipos, reportes de fallas y seguimiento de tickets"
      />

      <div className="mb-6 inline-flex rounded-lg border border-slate-200 bg-white p-1 shadow-sm">
        <button
          type="button"
          onClick={() => setTab('equipment')}
          className={`rounded-md px-4 py-2 text-sm font-medium transition-colors ${
            tab === 'equipment' ? 'bg-brand-600 text-white' : 'text-slate-600 hover:bg-slate-50'
          }`}
        >
          Equipos
        </button>
        <button
          type="button"
          onClick={() => setTab('tickets')}
          className={`rounded-md px-4 py-2 text-sm font-medium transition-colors ${
            tab === 'tickets' ? 'bg-brand-600 text-white' : 'text-slate-600 hover:bg-slate-50'
          }`}
        >
          Tickets
        </button>
      </div>

      {tab === 'equipment' ? (
        <MaintenanceEquipmentPanel onReportTicket={reportTicket} />
      ) : (
        <MaintenanceTicketsPanel initialEquipmentId={ticketEquipment} />
      )}
    </div>
  );
}
