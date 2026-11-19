import { useRef } from 'react';
import { Printer } from 'lucide-react';
import Modal from './ui/Modal';
import Button from './ui/Button';

export default function ReceiptPreview({ receipt, onClose }) {
  const frameRef = useRef(null);

  const print = () => {
    frameRef.current?.contentWindow?.focus();
    frameRef.current?.contentWindow?.print();
  };

  return (
    <Modal
      isOpen={!!receipt}
      onClose={onClose}
      title={receipt?.title || 'Recibo'}
      size="xl"
    >
      {receipt?.html ? (
        <div className="space-y-3">
          <iframe
            ref={frameRef}
            title="Vista previa del recibo"
            srcDoc={receipt.html}
            className="h-[65vh] w-full rounded-lg border border-slate-200 bg-white"
          />
          <div className="flex justify-end gap-2">
            <Button variant="ghost" onClick={onClose}>
              Cerrar
            </Button>
            <Button icon={Printer} onClick={print}>
              Imprimir
            </Button>
          </div>
        </div>
      ) : (
        <p className="text-sm text-slate-500">No se pudo cargar el recibo.</p>
      )}
    </Modal>
  );
}
