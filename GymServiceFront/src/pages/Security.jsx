import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { QRCodeSVG } from 'qrcode.react';
import {
  ShieldCheck, ShieldOff, KeyRound, Copy, CheckCircle2, AlertTriangle, RefreshCw, Lock
} from 'lucide-react';
import api from '../services/api';
import { useAuth } from '../context/AuthContext';
import PageHeader from '../components/ui/PageHeader';
import Card from '../components/ui/Card';
import Button from '../components/ui/Button';
import Badge from '../components/ui/Badge';
import Input from '../components/ui/Input';

export default function Security() {
  const { logout } = useAuth();
  const navigate = useNavigate();
  const [status, setStatus] = useState(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const [setup, setSetup] = useState(null);
  const [activationDone, setActivationDone] = useState(false);
  const [code, setCode] = useState('');
  const [disableCode, setDisableCode] = useState('');
  const [copied, setCopied] = useState('');

  const [passwordForm, setPasswordForm] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });
  const [passwordBusy, setPasswordBusy] = useState(false);
  const [passwordError, setPasswordError] = useState('');

  const loadStatus = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/auth/mfa/status');
      setStatus(response.data);
    } catch (err) {
      setError(err.response?.data?.message || 'No se pudo consultar el estado de MFA.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadStatus();
  }, [loadStatus]);

  const copy = async (value, key) => {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(key);
      window.setTimeout(() => setCopied(''), 2000);
    } catch {
      setError('No se pudo copiar al portapapeles. Copia el texto manualmente.');
    }
  };

  const startSetup = async () => {
    setBusy(true);
    setError('');
    setNotice('');
    try {
      const response = await api.post('/auth/mfa/setup');
      setSetup(response.data);
      setActivationDone(false);
      setCode('');
    } catch (err) {
      setError(err.response?.data?.message || 'No se pudo iniciar la configuración de MFA.');
    } finally {
      setBusy(false);
    }
  };

  const confirmActivation = async (e) => {
    e.preventDefault();
    setBusy(true);
    setError('');
    try {
      await api.post('/auth/mfa/verify', { code });
      setActivationDone(true);
      setNotice('MFA activado correctamente.');
      await loadStatus();
    } catch (err) {
      setError(err.response?.data?.message || 'Código inválido. Inténtalo de nuevo.');
    } finally {
      setBusy(false);
    }
  };

  const finishSetup = () => {
    setSetup(null);
    setActivationDone(false);
    setCode('');
    setNotice('');
  };

  const disableMfa = async (e) => {
    e.preventDefault();
    setBusy(true);
    setError('');
    setNotice('');
    try {
      await api.post('/auth/mfa/disable', { code: disableCode });
      setDisableCode('');
      setNotice('MFA desactivado.');
      await loadStatus();
    } catch (err) {
      setError(err.response?.data?.message || 'No se pudo desactivar MFA.');
    } finally {
      setBusy(false);
    }
  };

  const changePassword = async (e) => {
    e.preventDefault();
    setPasswordError('');

    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setPasswordError('La confirmación no coincide con la nueva contraseña.');
      return;
    }

    if (passwordForm.newPassword.length < 8) {
      setPasswordError('La nueva contraseña debe tener al menos 8 caracteres.');
      return;
    }

    setPasswordBusy(true);
    try {
      await api.post('/auth/change-password', {
        currentPassword: passwordForm.currentPassword,
        newPassword: passwordForm.newPassword
      });
      logout();
      navigate('/login', { replace: true });
    } catch (err) {
      setPasswordError(err.response?.data?.message || 'No se pudo cambiar la contraseña.');
    } finally {
      setPasswordBusy(false);
    }
  };

  const enabled = !!status?.enabled;

  return (
    <div>
      <PageHeader
        title="Seguridad"
        subtitle="Verificación en dos pasos para tu cuenta"
        actions={
          <Button variant="ghost" icon={RefreshCw} onClick={loadStatus} loading={loading}>
            Actualizar
          </Button>
        }
      />

      {error && (
        <div className="mb-4 flex items-start gap-3 rounded-lg border border-danger-200 bg-danger-50 p-4">
          <AlertTriangle className="mt-0.5 h-5 w-5 flex-shrink-0 text-danger-500" />
          <p className="text-sm text-danger-700">{error}</p>
        </div>
      )}

      {notice && (
        <div className="mb-4 flex items-start gap-3 rounded-lg border border-success-200 bg-success-50 p-4">
          <CheckCircle2 className="mt-0.5 h-5 w-5 flex-shrink-0 text-success-600" />
          <p className="text-sm text-success-700">{notice}</p>
        </div>
      )}

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="lg:col-span-2">
          {setup ? (
            <Card
              title={activationDone ? 'MFA activado' : 'Configurar aplicación de autenticación'}
              subtitle={
                activationDone
                  ? 'Guarda tus códigos de respaldo en un lugar seguro.'
                  : 'Escanea el código con Google Authenticator, Microsoft Authenticator o 1Password.'
              }
            >
              {!activationDone && (
                <>
                  <div className="flex flex-col items-center gap-4 sm:flex-row sm:items-start">
                    <div className="rounded-xl border border-slate-200 bg-white p-3">
                      <QRCodeSVG value={setup.totpUri} size={168} level="M" />
                    </div>
                    <div className="min-w-0 flex-1">
                      <p className="text-sm text-slate-600">
                        ¿No puedes escanear el código? Introduce esta clave manualmente:
                      </p>
                      <div className="mt-2 flex items-center gap-2">
                        <code className="min-w-0 flex-1 break-all rounded-lg bg-slate-100 px-3 py-2 font-mono text-xs text-slate-700">
                          {setup.secret}
                        </code>
                        <Button
                          size="sm"
                          variant="ghost"
                          icon={copied === 'secret' ? CheckCircle2 : Copy}
                          onClick={() => copy(setup.secret, 'secret')}
                        >
                          {copied === 'secret' ? 'Copiada' : 'Copiar'}
                        </Button>
                      </div>
                      <p className="mt-3 text-xs text-slate-400">
                        Tipo: TOTP · 6 dígitos · 30 segundos · Emisor: CentroFit
                      </p>
                    </div>
                  </div>

                  <div className="mt-6 rounded-lg border border-warning-200 bg-warning-50 p-4">
                    <p className="text-sm font-semibold text-warning-700">Códigos de respaldo</p>
                    <p className="mt-1 text-xs text-warning-700">
                      Cada código se puede usar una sola vez si pierdes el teléfono. Guárdalos ahora:
                      no se volverán a mostrar.
                    </p>
                    <div className="mt-3 grid grid-cols-2 gap-2 sm:grid-cols-3">
                      {setup.backupCodes.map((backupCode) => (
                        <code key={backupCode} className="rounded bg-white/80 px-2 py-1.5 text-center font-mono text-xs text-slate-700">
                          {backupCode}
                        </code>
                      ))}
                    </div>
                    <Button
                      size="sm"
                      variant="outline"
                      className="mt-3"
                      icon={copied === 'codes' ? CheckCircle2 : Copy}
                      onClick={() => copy(setup.backupCodes.join('\n'), 'codes')}
                    >
                      {copied === 'codes' ? 'Copiados' : 'Copiar códigos'}
                    </Button>
                  </div>

                  <form onSubmit={confirmActivation} className="mt-6 flex flex-col gap-3 sm:flex-row sm:items-end">
                    <Input
                      label="Código de 6 dígitos"
                      value={code}
                      onChange={(e) => setCode(e.target.value.replace(/[^0-9]/g, ''))}
                      placeholder="000000"
                      inputMode="numeric"
                      autoComplete="one-time-code"
                      className="sm:max-w-xs"
                      required
                    />
                    <Button type="submit" loading={busy} icon={ShieldCheck}>
                      Activar MFA
                    </Button>
                  </form>
                </>
              )}

              {activationDone && (
                <>
                  <div className="mb-4 rounded-lg border border-warning-200 bg-warning-50 p-4">
                    <p className="text-sm font-semibold text-warning-700">Guarda estos códigos de respaldo</p>
                    <p className="mt-1 text-xs text-warning-700">
                      No se volverán a mostrar. Sin el teléfono y sin un código de respaldo no podrás entrar.
                    </p>
                    <div className="mt-3 grid grid-cols-2 gap-2 sm:grid-cols-3">
                      {setup.backupCodes.map((backupCode) => (
                        <code key={backupCode} className="rounded bg-white/80 px-2 py-1.5 text-center font-mono text-xs text-slate-700">
                          {backupCode}
                        </code>
                      ))}
                    </div>
                    <Button
                      size="sm"
                      variant="outline"
                      className="mt-3"
                      icon={copied === 'codes' ? CheckCircle2 : Copy}
                      onClick={() => copy(setup.backupCodes.join('\n'), 'codes')}
                    >
                      {copied === 'codes' ? 'Copiados' : 'Copiar códigos'}
                    </Button>
                  </div>
                  <Button icon={CheckCircle2} onClick={finishSetup}>
                    He guardado los códigos
                  </Button>
                </>
              )}
            </Card>
          ) : (
            <Card>
              <div className="flex items-start gap-4">
                <div className={`flex h-12 w-12 flex-shrink-0 items-center justify-center rounded-xl ${enabled ? 'bg-success-50 text-success-600' : 'bg-slate-100 text-slate-500'}`}>
                  {enabled ? <ShieldCheck className="h-6 w-6" /> : <ShieldOff className="h-6 w-6" />}
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <h3 className="text-base font-semibold text-slate-800">Verificación en dos pasos</h3>
                    {enabled ? (
                      <Badge variant="success" dot>Activa</Badge>
                    ) : (
                      <Badge variant="neutral" dot>Inactiva</Badge>
                    )}
                  </div>
                  <p className="mt-1 text-sm text-slate-500">
                    {enabled
                      ? 'Al iniciar sesión se pedirá un código de tu aplicación de autenticación después de la contraseña.'
                      : 'Añade un segundo factor con una aplicación de autenticación. Reduce el riesgo si tu contraseña se filtra.'}
                  </p>

                  {enabled ? (
                    <div className="mt-4 space-y-4">
                      <div className="rounded-lg bg-slate-50 p-4 text-sm text-slate-600">
                        <p>
                          Códigos de respaldo disponibles:{' '}
                          <span className="font-semibold text-slate-800">{status?.backupCodesRemaining ?? 0}</span>
                        </p>
                        {status?.backupCodesRemaining === 0 && (
                          <p className="mt-1 text-xs text-warning-600">
                            Ya no te quedan códigos de respaldo. Desactiva y vuelve a configurar MFA para generar nuevos.
                          </p>
                        )}
                      </div>

                      <form onSubmit={disableMfa} className="rounded-lg border border-danger-200 bg-danger-50/50 p-4">
                        <p className="text-sm font-semibold text-slate-700">Desactivar MFA</p>
                        <p className="mt-1 text-xs text-slate-500">
                          Introduce un código válido de tu aplicación o un código de respaldo para confirmar.
                        </p>
                        <div className="mt-3 flex flex-col gap-3 sm:flex-row sm:items-end">
                          <Input
                            label="Código de verificación"
                            value={disableCode}
                            onChange={(e) => setDisableCode(e.target.value.replace(/[^0-9]/g, ''))}
                            placeholder="000000"
                            inputMode="numeric"
                            autoComplete="one-time-code"
                            className="sm:max-w-xs"
                            required
                          />
                          <Button type="submit" variant="danger" loading={busy}>Desactivar</Button>
                        </div>
                      </form>
                    </div>
                  ) : (
                    <div className="mt-4">
                      {status?.available === false ? (
                        <p className="text-sm text-danger-600">
                          La configuración de cifrado de MFA no está disponible en el servidor.
                        </p>
                      ) : (
                        <Button icon={ShieldCheck} onClick={startSetup} loading={busy}>
                          Activar MFA
                        </Button>
                      )}
                    </div>
                  )}
                </div>
              </div>
            </Card>
          )}

          <Card
            title="Cambiar contraseña"
            subtitle="Al cambiarla se cerrarán todas tus sesiones y deberás iniciar sesión de nuevo."
            className="mt-4"
          >
            <form onSubmit={changePassword} className="space-y-4">
              <Input
                label="Contraseña actual"
                type="password"
                value={passwordForm.currentPassword}
                onChange={(e) => setPasswordForm({ ...passwordForm, currentPassword: e.target.value })}
                autoComplete="current-password"
                required
              />
              <div className="grid gap-4 sm:grid-cols-2">
                <Input
                  label="Nueva contraseña"
                  type="password"
                  value={passwordForm.newPassword}
                  onChange={(e) => setPasswordForm({ ...passwordForm, newPassword: e.target.value })}
                  autoComplete="new-password"
                  placeholder="Mínimo 8 caracteres"
                  required
                />
                <Input
                  label="Confirmar nueva contraseña"
                  type="password"
                  value={passwordForm.confirmPassword}
                  onChange={(e) => setPasswordForm({ ...passwordForm, confirmPassword: e.target.value })}
                  autoComplete="new-password"
                  required
                />
              </div>

              {passwordError && (
                <div className="flex items-start gap-2 rounded-lg border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700">
                  <AlertTriangle className="mt-0.5 h-4 w-4 flex-shrink-0" />
                  <span>{passwordError}</span>
                </div>
              )}

              <Button type="submit" icon={Lock} loading={passwordBusy}>
                Actualizar contraseña
              </Button>
            </form>
          </Card>
        </div>

        <Card title="Cómo funciona">
          <ul className="space-y-3 text-sm text-slate-600">
            <li className="flex gap-3">
              <KeyRound className="mt-0.5 h-4 w-4 flex-shrink-0 text-brand-500" />
              <span>La contraseña sigue siendo obligatoria. MFA añade un código temporal de 30 segundos.</span>
            </li>
            <li className="flex gap-3">
              <ShieldCheck className="mt-0.5 h-4 w-4 flex-shrink-0 text-brand-500" />
              <span>Los códigos de respaldo son de un solo uso y sirven si pierdes el teléfono.</span>
            </li>
            <li className="flex gap-3">
              <AlertTriangle className="mt-0.5 h-4 w-4 flex-shrink-0 text-warning-500" />
              <span>Guarda los códigos fuera del servidor. Sin ellos, la recuperación requiere un administrador.</span>
            </li>
          </ul>
        </Card>
      </div>
    </div>
  );
}
