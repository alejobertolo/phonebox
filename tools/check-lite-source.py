#!/usr/bin/env python3
"""Static fail-closed regression checks for PhoneBox Lite.

This checker only reads source text. It does not compile, execute target code,
make network requests or prove runtime security. Run before build/review.
"""

from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[1]


def source(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


class LiteSourceContract(unittest.TestCase):
    def test_minimal_host_registers_only_eight_read_only_routes(self):
        text = source("agent/PhoneBox.Agent/Program.cs")
        endpoints = re.findall(r'app\.Map(\w+)\("([^"]+)"', text)
        self.assertEqual(set(endpoints), {
            ("Get", "/"), ("Get", "/panel/{*file}"), ("Get", "/favicon.ico"),
            ("Get", "/api/catalog"), ("Get", "/api/settings"),
            ("Get", "/api/config"), ("Get", "/api/info"), ("Get", "/api/state"),
        })
        self.assertEqual(len(endpoints), 8)
        self.assertIn("LiteSafety.Allows(ctx.Request.Method", text)
        self.assertIn("k.Listen(IPAddress.Loopback, endpoint.Port)", text)
        self.assertNotIn("IPAddress.Any", text)
        self.assertEqual(text.count("AddHostedService("), 2)
        for forbidden in ("MapPost(", "MapPut(", "MapDelete(", "UpdateService",
                          "ControlDeck", "CommandRunner", "ProcessMonitor"):
            self.assertNotIn(forbidden, text, forbidden)

    def test_policy_and_catalog_are_restricted(self):
        policy = source("agent/PhoneBox.Agent/LiteSafety.cs")
        self.assertIn('if (method != "GET"', policy)
        self.assertIn('"chart", "meters", "stat", "gauge"', policy)
        self.assertIn("Auto = false", policy)
        self.assertIn("Enabled = false", policy)
        self.assertIn("PhoneApprovals = false", policy)
        self.assertIn("HomeScreen = false", policy)
        self.assertIn("KeepAwake = false", policy)

    def test_application_files_do_not_mix_with_upstream(self):
        app = source("agent/PhoneBox.Agent/AppPaths.cs")
        gradle = source("android/app/build.gradle.kts")
        adb = source("agent/PhoneBox.Agent/AdbReverse.cs")
        self.assertIn('AppName = "PhoneBoxLite"', app)
        self.assertIn('applicationId = "net.alejobertolo.phoneboxlite"', gradle)
        self.assertIn("net.alejobertolo.phoneboxlite", adb)

    def test_android_transport_is_usb_and_read_only(self):
        transport = source("android/app/src/main/java/net/oxog/phonebox/deskpanel/AgentLink.kt")
        self.assertIn("val path = pathAndQuery.substringBefore('?')", transport)
        self.assertIn('method != "GET" || path !in API_READS', transport)
        self.assertIn('private const val USB_BASE = "http://127.0.0.1:8765"', transport)
        self.assertIn("connection.instanceFollowRedirects = false", transport)
        self.assertNotIn("HttpsURLConnection", transport)
        self.assertNotIn("fun wifi(", transport)
        self.assertNotIn("fun claim(", transport)
        self.assertNotIn("/api/pair", transport)

    def test_webview_and_manifest_have_no_external_route(self):
        activity = source("android/app/src/main/java/net/oxog/phonebox/deskpanel/MainActivity.kt")
        manifest = source("android/app/src/main/AndroidManifest.xml")
        self.assertIn("shouldOverrideUrlLoading", activity)
        self.assertIn("assetLoader.shouldInterceptRequest(url) ?: return FORBIDDEN()", activity)
        self.assertIn("setWebContentsDebuggingEnabled(false)", activity)
        self.assertNotIn("handlePairingLink(", activity)
        self.assertNotIn("category.BROWSABLE", manifest)
        self.assertNotIn("activity-alias", manifest)

    def test_installers_are_non_operational(self):
        self.assertIn("exit 1", source("setup.sh"))
        self.assertIn('throw "PhoneBox Lite:', source("setup.ps1"))


if __name__ == "__main__":
    unittest.main(verbosity=2)
