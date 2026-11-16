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
  Se eliminó físicamente el transporte Wi-Fi/HTTPS y los procedimientos de emparejamiento del cliente.
  No reintenta por Wi-Fi ni inicia el stream de eventos; el panel utiliza el sondeo normal cada segundo.
- El WebView rechaza recursos externos. Se desactivan deep links de emparejamiento, modo launcher/home,
  fijación de pantalla, depuración WebView y controles nativos de home/switching de PCs.
- La APK tiene otro identificador: `net.alejobertolo.phoneboxlite`, para **no sobrescribir la aplicación original**.
- Los scripts `setup.sh` y `setup.ps1` terminan expresamente con error; no descargan ni ejecutan binarios.
- Los nombres de las unidades de disco se reemplazan por identificadores genéricos antes de enviarlos al teléfono.
- La descarga de Gradle 8.11.1 lleva SHA-256 fijado contra el checksum oficial de Gradle.
- `tools/check-lite-source.py` contiene ocho pruebas estáticas que impiden reintroducir rutas peligrosas, Wi-Fi o instaladores; **no reemplazan una compilación ni pruebas de ejecución**.
- No hay servicio de inicio de sesión de macOS: el agente se ejecutaría manualmente durante la prueba.

## Endpoints GET disponibles

`/` (editor de consulta), `/panel/*` (panel y recursos estáticos), `/favicon.ico`,
`/api/state`, `/api/config`, `/api/catalog`, `/api/settings`, `/api/info`.

**No existe ninguna ruta de escritura activa**. El editor original puede mostrarse en el navegador,
pero los cambios **no se pueden guardar todavía**: al pulsar Guardar el servidor devuelve 403.
Es una limitación deliberada de Lite v1. En una segunda versión podremos habilitar guardado local
mediante un endpoint separado con autenticación y validación estricta de solo widgets permitidos.

## Correcciones de endurecimiento (16/11/2026)

- **Datos locales independientes:** `AppPaths.AppName = "PhoneBoxLite"`. El agente no usa la carpeta
  `PhoneBox` de la versión upstream, ni lee sus archivos de configuración anteriores.
- **WebView sin navegación externa:** `MainActivity.shouldOverrideUrlLoading` bloquea cambios
  de página hacia orígenes o esquemas no autorizados. `shouldInterceptRequest` devuelve
  una respuesta explícita de denegación para cualquier recurso desconocido, también si
  usa el host virtual de activos. Esto evita que una ruta no atendida continúe como
  una solicitud real a Internet.
- **Prueba de regresión agregada** para verificar que las carpetas de configuración/cache
  sean `PhoneBoxLite`.

**Verificación disponible:** lectura del código y revisión estática de las rutas declaradas.
**No verificado:** ejecución real de la prueba nueva, compilación .NET/Gradle, tráfico de red
en Android, instalación por ADB y comportamiento de la interfaz.

## Riesgos pendientes y verificaciones necesarias

1. **Compilación y pruebas no ejecutadas**: hacen falta .NET SDK 10, JDK 17 y Android SDK Platform 35.
   El entorno actual no tiene los SDK y no puede descargarlos; no se ejecutaron tests de .NET, Gradle, ni el checker Python localmente.
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

Primero validaremos el contrato estático: `python3 tools/check-lite-source.py`.

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

## Resultado de CI, primera ejecución completa (16/11/2026)

La primera ejecución `43926698682` falló en Android (import `Intent`), .NET (`PairClaim`
ya no definido), tests UI del proyecto completo, y verificación de metadatos del release original.
Esas dos fallas de compilación se corrigieron en `a99aca4fa2470960a7063682b08f0a47fb056a7e`.

La segunda ejecución `43932260975` **compiló y validó Android exitosamente**. El
agente .NET **compiló y ejecutó 738 tests**, con **707 aprobados y 31 fallidos**. Estas fallas
son en pruebas heredadas que intentan activar funciones intencionalmente deshabilitadas
(Discord/OBS/comandos/configuración de templates/ADB con ruta arbitraria). Al detenerse en
`dotnet test`, esa ejecución **no llegó a cross-compilar el binario osx-arm64 ni ejecutar
las pruebas HTTP de seguridad**.

Por eso la tercera ejecución limita el test unitario de CI a `LiteSafetyTests`,
`NetworkRatesTests` y `EndpointTests`, y agrega smoke HTTP con rechazos de
escritura, ejecuciones, proxys, rutas no autorizadas, `Host` y `Origin` externos.
**Esto no cuenta como aprobación del conjunto completo de tests originales**. Las
31 incompatibilidades heredadas permanecen visibles en la ejecución anterior y
los tests originales no fueron borrados ni modificados para fingir resultados.

La validación en hardware sigue pendiente y es requisito de salida.
