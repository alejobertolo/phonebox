# PhoneBox Lite (experimental)

A minimal read-only fork of [PhoneBox](https://github.com/ersinkoc/phonebox) for using an older
Android phone as a USB-connected system-monitoring panel for macOS.

**Status: experimental development branch — NOT READY TO INSTALL.**
No one has compiled, executed or tested this Lite agent/APK on the target Mac and Android 9 phone yet.
Do not execute upstream installers, install upstream release binaries or grant additional permissions.

## Scope of the first version

- Beautiful PhoneBox panel UI, with one **System** page.
- CPU, RAM, disk and network statistics from the Mac.
- Android phone connects only over trusted **ADB USB reverse**.
- Local agent bound to `127.0.0.1`, GET-only HTTP API, no writes.
- No executable shortcuts, process kills, app control, remote command widgets, update service,
  webhooks, Wi-Fi/Tailscale, external feeds or account connections.
- APK uses a separate package name: `net.alejobertolo.phoneboxlite`.
- No automatic installation, macOS login item, root or reset.
- The original settings editor can be viewed, **but Save is intentionally disabled** (403).

## Safety review and tests

Read **[Lite security scope and outstanding checks](docs/LITE-SECURITY.md)**.

Before a device install, we still need a dependency review, .NET + Android compilation,
tests of every forbidden endpoint and network path, compatibility checks on macOS ARM / Android 9
and a source/build review. This code should not be described as safe or ready until those checks pass.

The original project and documentation remain available at
[ersinkoc/phonebox](https://github.com/ersinkoc/phonebox).
This derivative retains the upstream [MIT license](LICENSE) and required notices.

**Working branch:** `feat/lite-read-only-v1`. The fork's `main` branch remains unchanged.
