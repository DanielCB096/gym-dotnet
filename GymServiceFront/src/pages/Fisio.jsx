import { useState } from 'react';
import PageHeader from '../components/ui/PageHeader';
import FisioPackagesPanel from '../components/FisioPackagesPanel';
import FisioAppointmentsPanel from '../components/FisioAppointmentsPanel';

export default function Fisio() {
  const [tab, setTab] = useState('packages');
  const [appointmentMember, setAppointmentMember] = useState('');

  const viewAppointments = (memberId) => {
    setAppointmentMember(memberId ? String(memberId) : '');
    setTab('appointments');
  };

  return (
    <div className="animate-fade-in">
      <PageHeader
        title="Fisioterapia"
        subtitle="Paquetes de sesiones, citas y asistencia"
      />

      <div className="mb-6 inline-flex rounded-lg border border-slate-200 bg-white p-1 shadow-sm">
        <button
          type="button"
          onClick={() => setTab('packages')}
          className={`rounded-md px-4 py-2 text-sm font-medium transition-colors ${
            tab === 'packages' ? 'bg-brand-600 text-white' : 'text-slate-600 hover:bg-slate-50'
          }`}
        >
          Paquetes
        </button>
        <button
          type="button"
          onClick={() => setTab('appointments')}
          className={`rounded-md px-4 py-2 text-sm font-medium transition-colors ${
            tab === 'appointments' ? 'bg-brand-600 text-white' : 'text-slate-600 hover:bg-slate-50'
          }`}
        >
          Citas
        </button>
      </div>

      {tab === 'packages' ? (
        <FisioPackagesPanel onViewAppointments={viewAppointments} />
      ) : (
        <FisioAppointmentsPanel initialMemberId={appointmentMember} />
      )}
    </div>
  );
}
