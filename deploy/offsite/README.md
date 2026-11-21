# Respaldo offsite en OneDrive

Copia cifrada de los respaldos de GymService a OneDrive, separada de la copia
local de `D:\GymBackups`.

## Destino

`C:\Users\PC\OneDrive\GymServiceBackups`

- Los `.bak`/`.trn` se comprimen y cifran con **7-Zip AES-256** (`-mhe=on`,
  cabeceras cifradas) antes de copiarlos.
- OneDrive sincroniza automáticamente esa carpeta al cloud.
- No se suben archivos planos ni contraseñas.

## Contraseña

- Para la tarea: `C:\ProgramData\GymService\secrets\offsite-backup.pass`
  (solo `SYSTEM` y `Administradores`).
- Copia para el cliente: `C:\Users\PC\Desktop\CentroFit-offsite-backup-password.txt`
  (solo el usuario actual). Guárdala en un gestor de contraseñas y borra el archivo.
- **Sin la contraseña no se pueden restaurar los `.7z`.** Consérvala fuera del servidor.

## Tarea programada

`GymService-Offsite`:

- Se ejecuta como `SYSTEM` cada **15 minutos**.
- Usa `Sync-OffsiteBackups.ps1` instalado en
  `C:\Program Files\Centro Fit\GymService`.
- Si OneDrive no está en ejecución, la tarea falla (no se considera una copia offsite válida).
- Retención: **30 días** para `.bak.7z` y **7 días** para `.trn.7z`.
- Log: `C:\ProgramData\GymService\logs\offsite-backup.log`.

## Restaurar un respaldo cifrado

```powershell
# 1. Descomprimir/cifrar a una carpeta temporal
7z x "C:\Users\PC\OneDrive\GymServiceBackups\GymManager_YYYYMMDD_HHMMSS.bak.7z" -o"C:\Temp\GymRestore" -p"<contraseña>"

# 2. Verificar/restaurar con el script del servicio
cd "C:\Program Files\Centro Fit\GymService"
.\verify-backup.ps1 -BackupFile "C:\Temp\GymRestore\GymManager_YYYYMMDD_HHMMSS.bak"
```

## Reinstalar/ajustar

```powershell
# Copiar el script a la carpeta del servicio y registrar la tarea (Administrador)
Copy-Item .\Sync-OffsiteBackups.ps1 "C:\Program Files\Centro Fit\GymService\" -Force
.\Register-OffsiteTask.ps1
```

Para cambiar el destino o la retención, edita `Sync-OffsiteBackups.ps1` y
vuelve a copiarlo; la tarea no necesita re-registrarse.
