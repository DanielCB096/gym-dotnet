# Monitoreo de respaldos

Comprueba que los respaldos locales, el offsite de OneDrive y la clave MFA estén sanos.
Si detecta un problema, envía un correo al owner usando el SMTP configurado.

## Tareas

| Tarea | Cuándo | Qué hace |
|---|---|---|
| `GymService-BackupMonitor` | Diario 08:00 | Revisa tareas, disco, OneDrive, antigüedad de archivos y clave MFA |
| `GymService-RestoreVerify` | Domingos 03:00 | Descifra el último `.bak.7z` y prueba una restauración real en base temporal |

Ambas corren como `SYSTEM`. La verificación usa `verify-backup.ps1` y elimina la base
temporal al terminar.

## Qué se revisa

- `GymService-Full`: último resultado 0 y ejecución en las últimas 26 horas.
- `GymService-Log`: último resultado 0 y ejecución en los últimos 30 minutos.
- `GymService-Offsite`: último resultado 0 y ejecución en los últimos 90 minutos.
- Espacio libre en `D:\GymBackups` (mínimo 5 GB).
- OneDrive en ejecución.
- Archivos `.7z` recientes en la carpeta offsite.
- Copia `mfa-key-*.7z` de menos de 32 días.
- Con `-VerifyRestore`: restauración real del último respaldo completo.

## Uso

```powershell
# Diagnóstico sin enviar correo
.\Test-GymBackupHealth.ps1 -DryRun

# Diagnóstico + restauración real, sin correo
.\Test-GymBackupHealth.ps1 -DryRun -VerifyRestore

# Forzar un correo de estado OK
.\Test-GymBackupHealth.ps1 -AlwaysNotify

# Registrar/eliminar las tareas (Administrador)
.\Register-GymBackupMonitor.ps1
.\Register-GymBackupMonitor.ps1 -Unregister
```

Log: `C:\ProgramData\GymService\logs\backup-monitor.log`.

## Respaldo completo diario

El respaldo completo tiene `WakeToRun` habilitado para que el equipo despierte a las
02:00. El 2026-11-22 el monitor detectó que la ventana se había saltado; se ejecutó
el respaldo manualmente y se corrigió la tarea. La tarea de log y offsite no despiertan
el equipo para no interrumpirlo cada 15 minutos.
