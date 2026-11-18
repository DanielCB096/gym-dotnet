import { useState, useEffect } from 'react';
import api from '../services/api';

export default function Inventory() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get('/inventory')
      .then((r) => setItems(r.data?.items || []))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="text-center py-12 text-gray-500">Cargando inventario...</div>;

  return (
    <div>
      <h2 className="text-2xl font-bold text-gray-800 mb-6">Inventario</h2>
      <div className="bg-white rounded-xl shadow-sm overflow-hidden">
        <table className="w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Producto</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Categoría</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Precio</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Stock</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {items.length === 0 ? (
              <tr><td colSpan="4" className="px-6 py-12 text-center text-gray-500">No hay productos</td></tr>
            ) : (
              items.map((item, i) => (
                <tr key={i} className="hover:bg-gray-50">
                  <td className="px-6 py-4">{item.productName || item.name}</td>
                  <td className="px-6 py-4">{item.category || '-'}</td>
                  <td className="px-6 py-4">${item.price?.toFixed(2)}</td>
                  <td className="px-6 py-4">{item.stock ?? '-'}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
