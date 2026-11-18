import { useState, useEffect } from 'react';
import api from '../services/api';

export default function Members() {
  const [members, setMembers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [success, setSuccess] = useState('');
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    membershipType: 'basic',
  });

  useEffect(() => {
    loadMembers();
  }, []);

  const loadMembers = async () => {
    try {
      const response = await api.get('/members');
      setMembers(response.data?.data || response.data?.items || []);
    } catch (err) {
      console.error('Error cargando miembros:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setSuccess('');
    try {
      await api.post('/members', formData);
      setFormData({ firstName: '', lastName: '', email: '', phone: '', membershipType: 'basic' });
      setShowForm(false);
      setSuccess('Miembro creado exitosamente');
      setTimeout(() => setSuccess(''), 3000);
      loadMembers();
    } catch (err) {
      console.error('Error creando miembro:', err);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <svg className="animate-spin w-8 h-8 mx-auto text-primary-600 mb-3" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <p className="text-slate-500">Cargando miembros...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="animate-fade-in">
      {/* Page header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
        <div>
          <h2 className="text-2xl lg:text-3xl font-bold text-slate-800 tracking-tight">Miembros</h2>
          <p className="text-slate-500 mt-1">{members.length} miembro{members.length !== 1 ? 's' : ''} registrado{members.length !== 1 ? 's' : ''}</p>
        </div>
        <button
          onClick={() => setShowForm(!showForm)}
          className={`inline-flex items-center gap-2 px-5 py-2.5 rounded-xl font-medium text-sm transition-all duration-200 shadow-sm ${
            showForm
              ? 'bg-slate-100 text-slate-700 hover:bg-slate-200'
              : 'bg-gradient-to-r from-primary-600 to-primary-700 text-white hover:from-primary-700 hover:to-primary-800 shadow-primary-600/25'
          }`}
        >
          {showForm ? (
            <>
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
              Cancelar
            </>
          ) : (
            <>
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
              </svg>
              Nuevo miembro
            </>
          )}
        </button>
      </div>

      {/* Success message */}
      {success && (
        <div className="mb-6 p-4 bg-emerald-50 border border-emerald-200 rounded-xl flex items-center gap-3 animate-fade-in">
          <svg className="w-5 h-5 text-emerald-500 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p className="text-sm text-emerald-700">{success}</p>
        </div>
      )}

      {/* Form */}
      {showForm && (
        <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-6 mb-6 animate-fade-in">
          <h3 className="text-lg font-semibold text-slate-800 mb-5">Datos del miembro</h3>
          <form onSubmit={handleSubmit} className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Nombre</label>
              <input
                type="text"
                value={formData.firstName}
                onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                className="w-full px-4 py-2.5 border border-slate-200 rounded-xl focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none transition bg-slate-50 focus:bg-white"
                placeholder="Juan"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Apellido</label>
              <input
                type="text"
                value={formData.lastName}
                onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                className="w-full px-4 py-2.5 border border-slate-200 rounded-xl focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none transition bg-slate-50 focus:bg-white"
                placeholder="Pérez"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Email</label>
              <input
                type="email"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                className="w-full px-4 py-2.5 border border-slate-200 rounded-xl focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none transition bg-slate-50 focus:bg-white"
                placeholder="juan@email.com"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Teléfono</label>
              <input
                type="tel"
                value={formData.phone}
                onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                className="w-full px-4 py-2.5 border border-slate-200 rounded-xl focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none transition bg-slate-50 focus:bg-white"
                placeholder="555-1234"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1.5">Plan</label>
              <select
                value={formData.membershipType}
                onChange={(e) => setFormData({ ...formData, membershipType: e.target.value })}
                className="w-full px-4 py-2.5 border border-slate-200 rounded-xl focus:ring-2 focus:ring-primary-500 focus:border-primary-500 outline-none transition bg-slate-50 focus:bg-white"
              >
                <option value="basic">Básico</option>
                <option value="premium">Premium</option>
                <option value="vip">VIP</option>
              </select>
            </div>
            <div className="flex items-end">
              <button type="submit" className="w-full bg-gradient-to-r from-emerald-500 to-emerald-600 hover:from-emerald-600 hover:to-emerald-700 text-white font-medium px-4 py-2.5 rounded-xl transition-all duration-200 shadow-sm shadow-emerald-500/25">
                Guardar miembro
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Table */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
        {members.length === 0 ? (
          <div className="px-6 py-16 text-center">
            <div className="w-16 h-16 mx-auto mb-4 rounded-2xl bg-slate-50 flex items-center justify-center">
              <svg className="w-8 h-8 text-slate-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2 M9 11a4 4 0 100-8 4 4 0 000 8zm14 10v-2a4 4 0 00-3-3.87 M23 21v-2a4 4 0 00-4-4" />
              </svg>
            </div>
            <p className="text-slate-500 font-medium">No hay miembros registrados</p>
            <p className="text-slate-400 text-sm mt-1">Agrega el primer miembro con el botón de arriba</p>
          </div>
        ) : (
          <div className="table-responsive">
            <table className="w-full">
              <thead>
                <tr className="bg-slate-50 border-b border-slate-100">
                  <th className="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Nombre</th>
                  <th className="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Email</th>
                  <th className="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider hidden sm:table-cell">Teléfono</th>
                  <th className="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Plan</th>
                  <th className="px-5 py-3.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wider">Estado</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-50">
                {members.map((member, i) => (
                  <tr key={i} className="hover:bg-slate-50/50 transition-colors">
                    <td className="px-5 py-4">
                      <div className="flex items-center gap-3">
                        <div className="w-8 h-8 rounded-full bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center text-xs font-bold text-white flex-shrink-0">
                          {(member.firstName || 'U').charAt(0).toUpperCase()}
                        </div>
                        <span className="text-sm font-medium text-slate-800">{member.firstName} {member.lastName}</span>
                      </div>
                    </td>
                    <td className="px-5 py-4 text-sm text-slate-600">{member.email || <span className="text-slate-300">—</span>}</td>
                    <td className="px-5 py-4 text-sm text-slate-600 hidden sm:table-cell">{member.phone || <span className="text-slate-300">—</span>}</td>
                    <td className="px-5 py-4">
                      <span className={`inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-medium ${
                        member.membershipType === 'vip' ? 'bg-amber-50 text-amber-700' :
                        member.membershipType === 'premium' ? 'bg-violet-50 text-violet-700' :
                        'bg-slate-50 text-slate-600'
                      }`}>
                        {member.membershipType || 'Básico'}
                      </span>
                    </td>
                    <td className="px-5 py-4">
                      <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-xs font-medium ${
                        member.status === 'active' ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-50 text-slate-600'
                      }`}>
                        <span className={`w-1.5 h-1.5 rounded-full ${member.status === 'active' ? 'bg-emerald-500' : 'bg-slate-400'}`}></span>
                        {member.status === 'active' ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
