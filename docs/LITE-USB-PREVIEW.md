# PhoneBox Lite — primera prueba USB (pendiente de autorización técnica)

**Estado:** guía preparada, **no ejecutar todavía** hasta que el build modificado,
la revisión de dependencias y los controles de red/hardware hayan sido verificados.

## Reglas
- Nunca ejecutar `setup.sh` o `setup.ps1`, ni descargar releases upstream.
- No hacer merge del PR #1 ni cambiar permisos de accesibilidad/macOS ni dar root al teléfono.
- Usar únicamente una APK propia compilada desde la rama Lite y un agente verificado.
- Android 9: autorizar USB debugging **solo para la Mac personal** y revocar la autorización después de la sesión si no se necesita.
- La app no necesita conexión Wi-Fi para acceder al agente; desactivar datos/Wi-Fi en la primera prueba es una comprobación útil.
- No conectarla con Odoo, correo, claves personales, servicios ni cuentas corporativas.

## Fase A: Mac (sin conectar el teléfono)

1. Aislar la carpeta Lite y verificar de dónde proviene el código/artefacto y su SHA256.
2. Confirmar que no hay listener en el puerto 8765: `lsof -nP -iTCP:8765 -sTCP:LISTEN`.
3. Ejecutar **manualmente**, desde una carpeta temporal, el agente revisado con una config
   exclusiva Lite; no instalar login items ni modificar Gatekeeper/quarantine.
4. Abrir `http://127.0.0.1:8765/api/state` localmente. Solo deben aparecer métricas;
   el endpoint de comando debe devolver 403.
5. Cerrar el agente con Ctrl+C y comprobar que el puerto quedó libre.

## Fase B: USB (solo después de aprobar la fase A)

1. Revisar el APK construido y firmado con nuestro package id
   `net.alejobertolo.phoneboxlite`. Instalarla **manualmente**, con consentimiento.
2. Conectar el Samsung a la Mac, autorizar temporalmente la depuración USB y comprobar
   que la herramienta de plataforma de Android es oficial: `adb version` y `adb devices -l`.
   Si aparecen dos dispositivos, especificar el número de serie; no ejecutar comandos masivos.
3. Iniciar el agente Lite manualmente en la Mac, luego crear el túnel voluntario
   `adb -s SERIAL reverse tcp:8765 tcp:8765`.
4. Abrir PhoneBox Lite manualmente desde el Samsung; verificar CPU/RAM/disco/red.
   El agente no debe abrir la app ni cambiar el brillo, modo inicio o bloqueo.
5. Cortar la prueba con `adb -s SERIAL reverse --remove tcp:8765`, cerrar la app,
   detener el agente (Ctrl+C) y quitar el cable.
6. Desactivar depuración USB cuando no se use.

**Sin autoarranque, root, reinicios, actualizaciones remotas ni cambios de ROM.**
**No deshabilitar Gatekeeper ni eliminar cuarentena para probar ejecutables sin revisar.**

## Criterios de paso a la prueba en dispositivos

- GitHub Actions de la revisión exacta aprobados.
- Verificación de procedencia y revisión de dependencias transitivas.
- Confirmación de ausencia de ejecución ADB automática.
- Un panel visible al abrir la app, sin tráfico externo y con endpoints de control denegados.
- Comportamiento al conectar/desconectar USB, sin cambios persistentes sobre el Samsung.

Estas instrucciones **no sustituyen la validación pendiente**.
