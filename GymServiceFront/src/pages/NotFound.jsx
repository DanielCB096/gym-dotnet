import { Link } from 'react-router-dom';
import { AlertTriangle, ArrowLeft } from 'lucide-react';
import Button from '../components/ui/Button';

function NotFound() {
  return (
    <div className="flex items-center justify-center min-h-[60vh] px-4">
      <div className="text-center max-w-md">
        <div className="flex justify-center mb-6">
          <div className="w-20 h-20 rounded-full bg-amber-50 flex items-center justify-center">
            <AlertTriangle className="w-10 h-10 text-amber-500" />
          </div>
        </div>
        <h1 className="text-6xl font-bold text-slate-800 mb-2">404</h1>
        <p className="text-xl font-medium text-slate-700 mb-2">Página no encontrada</p>
        <p className="text-sm text-slate-500 mb-8">
          La página que estás buscando no existe o ha sido movida a otra ubicación.
        </p>
        <Link to="/">
          <Button variant="primary" icon={ArrowLeft}>
            Volver al inicio
          </Button>
        </Link>
      </div>
    </div>
  );
}

export default NotFound;
