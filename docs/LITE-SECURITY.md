# PhoneBox Lite — seguridad y estado de la versión experimental

**Repositorio:** fork personal `alejobertolo/phonebox`, rama `feat/lite-read-only-v1`  
**Base:** PhoneBox original de ersinkoc, licencia MIT (se conserva `LICENSE` y los avisos de terceros).
**Revisión del fork antes de cambios:** `b57d682f4acc591fd475f09baf3665995ffaabf2`.  
**Equipo previsto:** MacBook Air Apple Silicon + Samsung con Android 9, conectados por USB.

> **ESTADO: CÓDIGO EN REVISIÓN, NO APTO TODAVÍA PARA INSTALAR.**
> Esta rama contiene cambios de seguridad y pruebas definidas, pero **no se ha compilado, ejecutado ni probado el agente
> macOS o la APK en dispositivos reales**. No instalar ni ejecutar antes de la revisión y los controles pendientes.

## Objetivo inicial

Usar el Samsung como panel visual de **CPU, RAM, disco y transferencia de red de la Mac**, sin exponer
controles de sistema, aplicaciones de trabajo, sesiones, archivos, credenciales ni integración con la nube.

## Qué cambió en esta rama

- El antiguo `Program.cs` se sustituyó por un host mínimo: **solo GET** a recursos de interfaz, estado reducido,
  catálogo filtrado, información básica del enlace USB y una configuración saneada.
- No se registran rutas de ejecución de comandos, macros, apagado/cierre de procesos, control multimedia,
  actualizaciones, emparejamiento Wi-Fi, webhooks, proxy HTTP, Home Assistant ni autorizaciones de herramientas.
- Se mantiene una **lista explícita de rutas permitidas** en `LiteSafety.cs`; todo método distinto de GET se rechaza.
  La política se aplica también al prefijo del panel `/agent/api`.
- El servidor escucha solo en `127.0.0.1`, sin listener en LAN/Tailscale. No se registran servicios en segundo
  plano salvo la recopilación de métricas locales y la conexión USB/ADB.
- La fuente de métricas evita sondeos GPU, llamadas a sensores locales por HTTP e integraciones externas.
- La configuración compartida con el teléfono se reconstruye desde cero: **una sola página System**,
  sin comandos, reglas, tokens de emparejamiento ni accesos a apps.
- Android solo solicita por HTTP local cuatro endpoints GET de telemetría/configuración mediante USB.
  No reintenta por Wi-Fi ni inicia el stream de eventos; el panel utiliza el sondeo normal cada segundo.
- El WebView rechaza recursos externos. Se desactivan deep links de emparejamiento, modo launcher/home,
  fijación de pantalla, depuración WebView y controles nativos de home/switching de PCs.
- La APK tiene otro identificador: `net.alejobertolo.phoneboxlite`, para **no sobrescribir la aplicación original**.
- Los scripts `setup.sh` y `setup.ps1` terminan expresamente con error; no descargan ni ejecutan binarios.
- No hay servicio de inicio de sesión de macOS: el agente se ejecutaría manualmente durante la prueba.

## Endpoints GET disponibles

`/` (editor de consulta), `/panel/*` (panel y recursos estáticos), `/favicon.ico`,
`/api/state`, `/api/config`, `/api/catalog`, `/api/settings`, `/api/info`.

**No existe ninguna ruta de escritura activa**. El editor original puede mostrarse en el navegador,
pero los cambios **no se pueden guardar todavía**: al pulsar Guardar el servidor devuelve 403.
Es una limitación deliberada de Lite v1. En una segunda versión podremos habilitar guardado local
mediante un endpoint separado con autenticación y validación estricta de solo widgets permitidos.

## Riesgos pendientes y verificaciones necesarias

1. **Compilación y pruebas no ejecutadas**: hacen falta .NET SDK 10, JDK 17 y Android SDK Platform 35.
   Los tests originales pueden asumir funcionalidades que esta variante intencionalmente eliminó.
2. **Supply chain**: revisar dependencias NuGet y Gradle, scripts de compilación, licencias y artefactos generados.
   Compilar desde fuente no elimina por sí solo los riesgos de dependencias y herramientas.
3. **Pruebas HTTP integradas**: intentar acciones prohibidas con GET/POST/PUT/DELETE, rutas normalizadas,
   variantes de `Origin`, cabeceras falsas, rutas `/agent/api` y config maliciosa; probar que ninguna se ejecuta.
4. **Pruebas de red**: comprobar escucha exclusiva de loopback, ausencia de DNS y tráfico externo,
   y que la app nunca conecta por Wi-Fi ni descarga fuentes externas.
5. **Prueba Android 9**: revisar el WebView, firmar la APK propia, instalarla por ADB manualmente
   y confirmar que no comparte el identificador de paquete con PhoneBox original.
6. **Prueba en Mac**: arrancar el agente manualmente desde una carpeta temporal y configuración aislada.
   Comparar CPU/RAM/discos/red con Monitor de Actividad y verificar desenchufar/reconectar el USB.
7. **Revisión independiente de seguridad** del conjunto, no solo del instalador.

**No ejecutar `setup.sh` ni `setup.ps1` del proyecto original, ni instalar los releases upstream**.
No restaurar las funciones bloqueadas antes de reexaminar su frontera de seguridad.

## Comandos que usaremos en un entorno de desarrollo aislado, después de revisar dependencias

```sh
dotnet test agent/PhoneBox.Agent.Tests
cd android && ./gradlew lintDebug assembleDebug
```

Las comprobaciones y compilaciones deben ejecutarse en un entorno aislado para código de terceros,
antes de copiar un ejecutable a la Mac principal. No hacen falta root, formateo ni cambios de ROM.

## Criterio para pasar a «Lite v1 utilizable»

Solo cuando estén verificados: build, pruebas negativas de rutas/acciones, tráfico de red,
compatibilidad Android 9/macOS ARM, no autostart, no instalaciones ni descargas inesperadas,
y pantalla System con métricas correctas. Hasta entonces, esta rama es **experimental**.
