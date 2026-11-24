import { useRef, useState } from 'react';
import { AlertCircle, Camera, Upload } from 'lucide-react';
import api from '../services/api';
import Modal from './ui/Modal';
import Button from './ui/Button';

const MAX_SIZE = 5 * 1024 * 1024;

export default function MemberPhotoModal({ member, onClose, onUploaded }) {
  const inputRef = useRef(null);
  const [file, setFile] = useState(null);
  const [preview, setPreview] = useState(member?.photoUrl || '');
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  if (!member) return null;

  const choose = (event) => {
    const selected = event.target.files?.[0];
    setError('');
    if (!selected) return;

    if (!['image/jpeg', 'image/png', 'image/webp'].includes(selected.type)) {
      setError('Usa una imagen JPG, PNG o WebP.');
      return;
    }

    if (selected.size > MAX_SIZE) {
      setError('La imagen no puede superar 5 MB.');
      return;
    }

    setFile(selected);
    setPreview(URL.createObjectURL(selected));
  };

  const upload = async () => {
    if (!file) return;

    setSaving(true);
    setError('');
    try {
      const form = new FormData();
      form.append('memberId', member.id);
      form.append('file', file);
      const response = await api.post('/members/photo', form);
      onUploaded(response.data?.photoUrl || '', response.data?.message);
      onClose();
    } catch (err) {
      setError(err.response?.data?.message || 'No se pudo subir la imagen.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal isOpen onClose={onClose} title={`Foto de ${member.firstName || 'socio'} ${member.lastName || ''}`} size="md">
      <div className="space-y-4">
        <div className="flex items-center gap-4">
          <div className="flex h-24 w-24 flex-shrink-0 items-center justify-center overflow-hidden rounded-xl border border-slate-200 bg-slate-50">
            {preview ? (
              <img src={preview} alt="" className="h-full w-full object-cover" />
            ) : (
              <Camera className="h-8 w-8 text-slate-400" />
            )}
          </div>
          <div className="min-w-0 flex-1">
            <p className="text-sm text-slate-600">
              Foto actual del socio. Se guarda en el servidor y aparece en la tabla y en la ficha impresa.
            </p>
            <input
              ref={inputRef}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              className="hidden"
              onChange={choose}
            />
            <Button
              variant="outline"
              size="sm"
              icon={Camera}
              className="mt-3"
              onClick={() => inputRef.current?.click()}
            >
              Elegir imagen
            </Button>
            {file && <p className="mt-2 truncate text-xs text-slate-400">{file.name}</p>}
          </div>
        </div>

        {error && (
          <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
            <AlertCircle className="mt-0.5 h-4 w-4 flex-shrink-0" />
            <span>{error}</span>
          </div>
        )}

        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={onClose} disabled={saving}>Cancelar</Button>
          <Button icon={Upload} onClick={upload} loading={saving} disabled={!file}>
            Subir foto
          </Button>
        </div>
      </div>
    </Modal>
  );
}
